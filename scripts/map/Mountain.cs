using Godot;
using System;

public class Mountain
{
    public Vector3 Center { get; }
    public float BaseY { get; set; }
    public Func<Vector3, float> BaseHeight { get; set; } = _ => 0f;

    private readonly Track _track;
    private readonly Vector3 _along;
    private readonly Vector3 _across;
    private readonly float _coreHalf;
    private readonly float _alongRadius;
    private readonly float _acrossRadius;
    private readonly float _innerRadius;
    private readonly float _innerSign;
    private readonly float _height;
    private readonly float _portalHeight;
    private readonly FastNoiseLite _noise;

    public Mountain(Track track, int centerIndex, float coreHalf, float alongRadius,
        float acrossRadius, float innerRadius, Vector3 mapCenter,
        float height, float portalHeight, int seed)
    {
        _track = track;
        Center = track.Points[centerIndex];
        _along = track.Tangent(centerIndex);
        _across = track.Right(centerIndex);
        _coreHalf = coreHalf;
        _alongRadius = alongRadius;
        _acrossRadius = acrossRadius;
        _innerRadius = innerRadius;
        _innerSign = _across.Dot(mapCenter - Center) >= 0f ? 1f : -1f;
        _height = height;
        _portalHeight = portalHeight;
        _noise = new FastNoiseLite() { Seed = seed, Frequency = 1f / 70f };
    }

    private float AlongOf(Vector3 p) => new Vector3(p.X - Center.X, 0, p.Z - Center.Z).Dot(_along);

    private float AcrossNormalized(float across, float margin)
    {
        float radius = Mathf.Sign(across) == _innerSign ? _innerRadius : _acrossRadius;
        return across / (radius + margin);
    }

    private float NormalizedDistance(Vector3 p, float margin)
    {
        var local = new Vector3(p.X - Center.X, 0, p.Z - Center.Z);
        float along = Mathf.Max(0f, Mathf.Abs(local.Dot(_along)) - _coreHalf);
        float a = along / (_alongRadius + margin);
        float c = AcrossNormalized(local.Dot(_across), margin);
        return Mathf.Sqrt(a * a + c * c);
    }

    public bool Covers(Vector3 p, float margin) => NormalizedDistance(p, margin) < 1f;

    public float FootprintDistance(Vector3 p) => NormalizedDistance(p, 0f);

    public float DistanceToCore(Vector3 p)
    {
        float along = Mathf.Clamp(AlongOf(p), -_coreHalf, _coreHalf);
        var closest = Center + _along * along;
        return new Vector2(p.X - closest.X, p.Z - closest.Z).Length();
    }

    public float HeightAt(Vector3 p)
    {
        var local = new Vector3(p.X - Center.X, 0, p.Z - Center.Z);
        float along = Mathf.Abs(local.Dot(_along));
        float c = AcrossNormalized(local.Dot(_across), 0f);
        if (Mathf.Abs(c) >= 1f)
            return 0f;

        float profile;
        if (along <= _coreHalf)
        {
            float t = 1f - along / _coreHalf;
            profile = _portalHeight + (_height - _portalHeight) * Smooth(t);
        }
        else
        {
            float u = (along - _coreHalf) / _alongRadius;
            if (u >= 1f)
                return 0f;
            profile = _portalHeight * (1f - Smooth(u));
        }

        float acrossFactor = 1f - c * c;
        acrossFactor *= acrossFactor;
        float n = _noise.GetNoise2D(p.X, p.Z) * 0.5f + 0.5f;

        return profile * acrossFactor * (0.85f + 0.15f * n);
    }

    private static float Smooth(float x) => x * x * (3f - 2f * x);

    public Transform3D TunnelTransform(float centerOffset, float scale, float shift)
    {
        float yaw = Mathf.Atan2(_along.X, _along.Z);
        var basis = new Basis(Vector3.Up, yaw).Scaled(Vector3.One * scale);
        var origin = Center + _along * (shift - centerOffset * scale);
        origin.Y = BaseY;
        return new Transform3D(basis, origin);
    }

    public ArrayMesh BuildTerrain(float cellSize, float corridorHalf)
    {
        var st = MeshUtil.Begin();
        float extent = Mathf.Max(_coreHalf + _alongRadius, Mathf.Max(_acrossRadius, _innerRadius));
        int cells = Mathf.CeilToInt(extent * 2f / cellSize);
        float ox = Center.X - extent;
        float oz = Center.Z - extent;

        var rel = new float[cells + 1, cells + 1];
        var abs = new float[cells + 1, cells + 1];
        for (int x = 0; x <= cells; x++)
        {
            for (int z = 0; z <= cells; z++)
            {
                var p = new Vector3(ox + x * cellSize, 0, oz + z * cellSize);
                rel[x, z] = HeightAt(p);
                abs[x, z] = rel[x, z] > 0f ? BaseHeight(p) + rel[x, z] : 0f;
            }
        }

        for (int x = 0; x < cells; x++)
        {
            for (int z = 0; z < cells; z++)
            {
                float r00 = rel[x, z], r10 = rel[x + 1, z], r01 = rel[x, z + 1], r11 = rel[x + 1, z + 1];
                if (Mathf.Max(Mathf.Max(r00, r10), Mathf.Max(r01, r11)) < 0.05f)
                    continue;

                var cellCenter = new Vector3(ox + (x + 0.5f) * cellSize, 0, oz + (z + 0.5f) * cellSize);
                bool inCorridor = _track.DistanceToTrack(cellCenter) < corridorHalf + cellSize;
                if (inCorridor && Mathf.Abs(AlongOf(cellCenter)) > _coreHalf)
                    continue;

                float x0 = ox + x * cellSize, x1 = x0 + cellSize;
                float z0 = oz + z * cellSize, z1 = z0 + cellSize;

                MeshUtil.AddQuad(st,
                    new Vector3(x0, Abs(x, z), z0), new Vector3(x1, Abs(x + 1, z), z0),
                    new Vector3(x1, Abs(x + 1, z + 1), z1), new Vector3(x0, Abs(x, z + 1), z1),
                    Normal(x, z), Normal(x + 1, z), Normal(x + 1, z + 1), Normal(x, z + 1));
            }
        }

        return st.Commit();

        float Abs(int x, int z)
        {
            if (rel[x, z] > 0f)
                return abs[x, z];
            return BaseHeight(new Vector3(ox + x * cellSize, 0, oz + z * cellSize));
        }

        Vector3 Normal(int x, int z)
        {
            float hl = Abs(Mathf.Max(x - 1, 0), z);
            float hr = Abs(Mathf.Min(x + 1, cells), z);
            float hd = Abs(x, Mathf.Max(z - 1, 0));
            float hu = Abs(x, Mathf.Min(z + 1, cells));
            return new Vector3(hl - hr, 2f * cellSize, hd - hu).Normalized();
        }
    }

    public ArrayMesh BuildCut(float corridorHalf, float openingHalf, float openingHeight, float cellSize)
    {
        var st = MeshUtil.Begin();
        int n = _track.Count;

        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            var pi = _track.Points[i];
            var pj = _track.Points[j];

            if (!Covers(pi, corridorHalf) && !Covers(pj, corridorHalf))
                continue;
            if (Mathf.Abs(AlongOf(pi)) <= _coreHalf && Mathf.Abs(AlongOf(pj)) <= _coreHalf)
                continue;

            for (int side = -1; side <= 1; side += 2)
            {
                var ri = _track.Right(i) * side;
                var rj = _track.Right(j) * side;
                var ei = pi + ri * corridorHalf;
                var ej = pj + rj * corridorHalf;

                float hi = WallHeight(pi, ri, corridorHalf, cellSize);
                float hj = WallHeight(pj, rj, corridorHalf, cellSize);
                if (hi <= 0f && hj <= 0f)
                    continue;

                MeshUtil.AddQuad(st, ei, ej, ej + Vector3.Up * hj, ei + Vector3.Up * hi, -ri);
            }
        }

        AddPortal(st, -1f, openingHalf, openingHeight, corridorHalf, 16);
        AddPortal(st, 1f, openingHalf, openingHeight, corridorHalf, 16);

        return st.Commit();
    }

    private float WallHeight(Vector3 point, Vector3 outward, float corridorHalf, float cellSize)
    {
        float near = HeightAt(point + outward * corridorHalf);
        float far = HeightAt(point + outward * (corridorHalf + cellSize));
        float h = Mathf.Max(near, far);
        return h < 0.2f ? 0f : h + 1f;
    }

    private void AddPortal(SurfaceTool st, float sign, float openingHalf, float openingHeight, float outerHalf, int segments)
    {
        var p = Center + _along * (_coreHalf * sign);
        p.Y = BaseY;
        var right = _across;
        var normal = _along * sign;
        float top = Mathf.Max(HeightAt(p), Mathf.Max(HeightAt(p + right * outerHalf), HeightAt(p - right * outerHalf))) + 2f;

        Vector3 ToWorld(Vector2 local) => p + right * local.X + Vector3.Up * local.Y;

        Vector2 Inner(int s)
        {
            float phi = Mathf.Pi * s / segments;
            return new Vector2(Mathf.Cos(phi) * openingHalf, Mathf.Sin(phi) * openingHeight);
        }

        Vector2 Outer(int s)
        {
            var dir = Inner(s);
            float scale = float.MaxValue;
            if (Mathf.Abs(dir.X) > 0.0001f)
                scale = Mathf.Min(scale, outerHalf / Mathf.Abs(dir.X));
            if (dir.Y > 0.0001f)
                scale = Mathf.Min(scale, top / dir.Y);
            return dir * scale;
        }

        bool OnSide(Vector2 v) => Mathf.Abs(Mathf.Abs(v.X) - outerHalf) < 0.01f && Mathf.Abs(v.Y - top) > 0.01f;
        bool OnTop(Vector2 v) => Mathf.Abs(v.Y - top) < 0.01f && Mathf.Abs(Mathf.Abs(v.X) - outerHalf) > 0.01f;

        for (int s = 0; s < segments; s++)
        {
            var e0 = Inner(s);
            var e1 = Inner(s + 1);
            var o0 = Outer(s);
            var o1 = Outer(s + 1);

            MeshUtil.AddQuad(st, ToWorld(e0), ToWorld(e1), ToWorld(o1), ToWorld(o0), normal);

            if ((OnSide(o0) && OnTop(o1)) || (OnTop(o0) && OnSide(o1)))
            {
                float sideX = OnSide(o0) ? o0.X : o1.X;
                var corner = new Vector2(Mathf.Sign(sideX) * outerHalf, top);
                MeshUtil.AddTriangle(st, ToWorld(o0), ToWorld(corner), ToWorld(o1), normal);
            }
        }
    }
}