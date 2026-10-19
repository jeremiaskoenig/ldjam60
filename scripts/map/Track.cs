using Godot;
using System;
using System.Collections.Generic;

public class Track
{
    public List<Vector3> Points { get; } = new();
    public List<float> Distances { get; } = new();
    public float Length { get; private set; }
    public int Count => Points.Count;

    public static Track Generate(RandomNumberGenerator rng, float mapSize, int controlPoints,
        float minRadius, float maxRadius, float sampleSpacing, float minClearance, int maxAttempts = 30)
    {
        Track track = null;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            track = new Track();
            track.Build(rng, mapSize, controlPoints, minRadius, maxRadius, sampleSpacing);
            if (track.IsValid(minClearance))
                return track;
        }

        GD.PushWarning("Keine saubere Strecke gefunden, nehme den letzten Versuch.");
        return track;
    }

    private void Build(RandomNumberGenerator rng, float mapSize, int n, float minRadius, float maxRadius, float spacing)
    {
        var center = new Vector3(mapSize / 2f, 0, mapSize / 2f);
        float half = mapSize / 2f;
        float angleOffset = rng.Randf() * Mathf.Tau;
        var control = new List<Vector3>();

        for (int k = 0; k < n; k++)
        {
            float angle = angleOffset + Mathf.Tau * (k + rng.RandfRange(-0.3f, 0.3f)) / n;
            float radius = rng.RandfRange(minRadius, maxRadius) * half;
            control.Add(center + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius);
        }

        for (int k = 0; k < n; k++)
        {
            var p0 = control[(k - 1 + n) % n];
            var p1 = control[k];
            var p2 = control[(k + 1) % n];
            var p3 = control[(k + 2) % n];
            int steps = Mathf.Max(2, Mathf.CeilToInt(p1.DistanceTo(p2) / spacing));

            for (int s = 0; s < steps; s++)
                Points.Add(p1.CubicInterpolate(p2, p0, p3, (float)s / steps));
        }

        RecomputeDistances();
    }

    private bool IsValid(float minClearance)
    {
        int n = Count;
        int skip = Mathf.CeilToInt(minClearance * 3f / (Length / n));

        for (int i = 0; i < n; i++)
        {
            for (int j = i + 1; j < n; j++)
            {
                int indexDist = Mathf.Min(j - i, n - (j - i));
                if (indexDist < skip)
                    continue;

                if (Points[i].DistanceTo(Points[j]) < minClearance)
                    return false;
            }
        }
        return true;
    }

    public Vector3 Tangent(int i) => (Points[(i + 1) % Count] - Points[(i - 1 + Count) % Count]).Normalized();
    public Vector3 Right(int i) => Tangent(i).Cross(Vector3.Up).Normalized();
    public float ProgressOf(int i) => Distances[i] / Length;

    public int IndexAtProgress(float progress)
    {
        float s = Mathf.PosMod(progress, 1f) * Length;
        int i = Distances.BinarySearch(s);
        return i >= 0 ? i : Mathf.Max(0, ~i - 1);
    }

    public int ClosestIndex(Vector3 position, out float distance)
    {
        int best = 0;
        float bestSq = float.MaxValue;

        for (int i = 0; i < Count; i++)
        {
            float dx = Points[i].X - position.X;
            float dz = Points[i].Z - position.Z;
            float sq = dx * dx + dz * dz;
            if (sq < bestSq)
            {
                bestSq = sq;
                best = i;
            }
        }

        distance = Mathf.Sqrt(bestSq);
        return best;
    }

    public float DistanceToTrack(Vector3 position) => DistanceToTrack(position, out _);

    public float DistanceToTrack(Vector3 position, out float height)
    {
        int index = ClosestIndex(position, out _);
        var p = new Vector2(position.X, position.Z);
        float best = float.MaxValue;
        height = Points[index].Y;

        for (int k = -1; k <= 0; k++)
        {
            int i0 = (index + k + Count) % Count;
            int i1 = (i0 + 1) % Count;
            var a = new Vector2(Points[i0].X, Points[i0].Z);
            var b = new Vector2(Points[i1].X, Points[i1].Z);
            var ab = b - a;
            float t = Mathf.Clamp((p - a).Dot(ab) / ab.LengthSquared(), 0f, 1f);
            float dist = p.DistanceTo(a + ab * t);

            if (dist < best)
            {
                best = dist;
                height = Mathf.Lerp(Points[i0].Y, Points[i1].Y, t);
            }
        }

        return best;
    }

    public Vector3 SampleAt(float s, out Vector3 tangent)
    {
        s = Mathf.PosMod(s, Length);
        int i = Distances.BinarySearch(s);
        if (i < 0)
            i = Mathf.Max(0, ~i - 1);

        int j = (i + 1) % Count;
        float end = j == 0 ? Length : Distances[j];
        float segLength = end - Distances[i];
        float t = segLength > 0f ? (s - Distances[i]) / segLength : 0f;

        tangent = (Points[j] - Points[i]).Normalized();
        return Points[i].Lerp(Points[j], t);
    }

    public void ApplyElevation(Func<Vector3, float> elevation)
    {
        for (int i = 0; i < Count; i++)
        {
            var p = Points[i];
            Points[i] = new Vector3(p.X, elevation(p), p.Z);
        }
        RecomputeDistances();
    }

    private void RecomputeDistances()
    {
        Distances.Clear();
        float total = 0f;
        for (int i = 0; i < Points.Count; i++)
        {
            Distances.Add(total);
            total += Points[i].DistanceTo(Points[(i + 1) % Points.Count]);
        }
        Length = total;
    }

    public void Straighten(int centerIndex, float halfLength, float blend)
    {
        var center = Points[centerIndex];
        var dir = Tangent(centerIndex);
        float centerDistance = Distances[centerIndex];

        for (int i = 0; i < Count; i++)
        {
            float d = Distances[i] - centerDistance;
            if (d > Length / 2f) d -= Length;
            if (d < -Length / 2f) d += Length;

            float ad = Mathf.Abs(d);
            if (ad > halfLength + blend)
                continue;

            float w = ad <= halfLength ? 1f : 1f - (ad - halfLength) / blend;
            w = w * w * (3f - 2f * w);
            Points[i] = Points[i].Lerp(center + dir * d, w);
        }

        RecomputeDistances();
    }
}