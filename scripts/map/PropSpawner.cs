using Godot;
using System;
using System.Collections.Generic;

public partial class PropSpawner : Node3D
{
    [ExportGroup("Westernstadt")]
    [Export] public Godot.Collections.Array<PackedScene> WesternBuildings { get; set; } = new();
    [Export] public float WesternBuildingRadius { get; set; } = 8f;
    [Export] public float WesternSetback { get; set; } = 10f;
    [Export] public float WesternSpacing { get; set; } = 3f;
    [Export(PropertyHint.Range, "-180,180,90")] public float WesternYawOffset { get; set; } = 0f;

    [ExportGroup("Zaun")]
    [Export] public Godot.Collections.Array<PackedScene> Fences { get; set; } = new();
    [Export] public float FenceLength { get; set; } = 2.5f;
    [Export] public float FenceRoadDistance { get; set; } = 5f;
    [Export] public float FenceBackMargin { get; set; } = 5f;
    [Export(PropertyHint.Range, "-180,180,90")] public float FenceYawOffset { get; set; } = 0f;
    [Export] public bool FenceOriginCentered { get; set; } = true;

    [ExportGroup("Stadt")]
    [Export] public Godot.Collections.Array<PackedScene> CityBuildings { get; set; } = new();
    [Export] public float BuildingRadius { get; set; } = 10f;
    [Export] public float BuildingSetback { get; set; } = 2f;
    [Export] public float BuildingSpacing { get; set; } = 3f;
    [Export] public int BuildingRows { get; set; } = 3;
    [Export] public bool CityBorderBuildings { get; set; } = true;
    [Export] public float CityBorderDepth { get; set; } = 50f;
    [Export] public float CityBorderReach { get; set; } = 250f;
    [Export(PropertyHint.Range, "-180,180,90")] public float ModelYawOffset { get; set; } = -180f;

    [ExportGroup("Wüste")]
    [Export] public Godot.Collections.Array<PackedScene> Cacti { get; set; } = new();
    [Export] public float CactusRadius { get; set; } = 1.5f;
    [Export] public float CactiPerKm2 { get; set; } = 120f;
    [Export] public Vector2 CactusScaleRange { get; set; } = new(0.8f, 1.4f);
    [Export] public float CactusCollisionRadius { get; set; } = 0.4f;

    [ExportGroup("Wald")]
    [Export] public Godot.Collections.Array<PackedScene> Trees { get; set; } = new();
    [Export] public float TreeRadius { get; set; } = 3f;
    [Export] public float TreesPerKm2 { get; set; } = 400f;
    [Export] public Vector2 TreeScaleRange { get; set; } = new(0.8f, 1.3f);
    [Export] public float TrunkRadius { get; set; } = 0.4f;

    [ExportGroup("Laternen")]
    [Export] public Godot.Collections.Array<PackedScene> StreetLamps { get; set; } = new();
    [Export] public float CityLampSpacing { get; set; } = 50f;
    [Export] public bool CityLampsBothSides { get; set; } = false;
    [Export] public float LampCurbDistance { get; set; } = 0.6f;
    [Export(PropertyHint.Range, "-180,180,90")] public float LampYawOffset { get; set; } = 0f;
    [Export] public float LampPoleRadius { get; set; } = 0.15f;

    [ExportGroup("Allgemein")]
    [Export] public float RoadClearance { get; set; } = 3f;
    [Export] public bool AutoCollision { get; set; } = true;
    [Export] public float CollisionHeight { get; set; } = 8f;

    private MapGenerator _map;
    private Node3D _container;
    private readonly RandomNumberGenerator _rng = new();
    private readonly List<(Vector2 pos, float radius)> _placed = new();

    public void Spawn(MapGenerator map)
    {
        _map = map;
        _rng.Seed = (ulong)GD.Hash($"{map.Seed}-props");
        _placed.Clear();

        _container?.QueueFree();
        _container = new Node3D() { Name = "Props" };
        AddChild(_container);

        int western = SpawnAlongTrack(WesternBuildings, p => _map.Biomes.IsInTown(p),
            1, WesternBuildingRadius, WesternSetback, WesternSpacing, WesternYawOffset, 0f);

        int fences = SpawnTownFences();

        int city = SpawnAlongTrack(CityBuildings, p => _map.Biomes.GetBiomeAt(p) == Biome.City,
            BuildingRows, BuildingRadius, BuildingSetback, BuildingSpacing, ModelYawOffset, _map.SidewalkWidth);

        int border = SpawnCityBorder();

        int lamps = SpawnLamps();

        int cacti = Scatter(Cacti, CactiPerKm2, CactusRadius, CactusScaleRange, CactusCollisionRadius,
            p => _map.Biomes.GetBiomeAt(p) == Biome.Desert && !_map.Biomes.IsInTown(p));

        int trees = Scatter(Trees, TreesPerKm2, TreeRadius, TreeScaleRange, TrunkRadius,
            p => _map.Biomes.GetBiomeAt(p) == Biome.Forest);

        GD.Print($"Props: {western} Western, {fences} Zaunteile, {city} Stadt, {border} Stadtrand, {lamps} Laternen, {cacti} Kakteen, {trees} Bäume");
    }

    private int SpawnAlongTrack(Godot.Collections.Array<PackedScene> scenes, Func<Vector3, bool> zone,
        int rows, float radius, float setback, float spacing, float yawOffset, float extraOffset)
    {
        if (scenes.Count == 0)
            return 0;

        int count = 0;
        var track = _map.Track;
        float step = radius * 2f + spacing;

        for (int row = 0; row < rows; row++)
        {
            float offset = _map.RoadWidth / 2f + extraOffset + setback + radius + row * step;

            for (float s = row * step / rows; s < track.Length; s += step)
            {
                var center = track.SampleAt(s, out var tangent);
                var right = tangent.Cross(Vector3.Up).Normalized();

                for (int side = -1; side <= 1; side += 2)
                {
                    var pos = center + right * offset * side;
                    if (!zone(pos) || !IsFree(pos, radius))
                        continue;

                    var node = Place(Pick(scenes), pos, -right * side, 1f, radius, yawOffset);
                    if (AutoCollision)
                        AddConvexCollision(node);
                    count++;
                }
            }
        }

        return count;
    }

    private int SpawnTownFences()
    {
        if (Fences.Count == 0)
            return 0;

        var track = _map.Track;
        float halfRoad = _map.RoadWidth / 2f;
        float front = halfRoad + FenceRoadDistance;
        float back = halfRoad + WesternSetback + WesternBuildingRadius * 2f + FenceBackMargin;
        float s0 = _map.TownStart * track.Length;
        float s1 = _map.TownEnd * track.Length;
        const float sampleStep = 10f;
        int count = 0;

        for (int side = -1; side <= 1; side += 2)
        {
            var outline = new List<Vector3>();

            for (float s = s0; s < s1; s += sampleStep)
                outline.Add(OffsetPoint(track, s, front * side));
            outline.Add(OffsetPoint(track, s1, front * side));

            for (float s = s1; s > s0; s -= sampleStep)
                outline.Add(OffsetPoint(track, s, back * side));
            outline.Add(OffsetPoint(track, s0, back * side));

            outline.Add(outline[0]);

            for (int i = 0; i < outline.Count - 1; i++)
                count += PlaceFenceLine(outline[i], outline[i + 1]);
        }

        return count;
    }

    private static Vector3 OffsetPoint(Track track, float s, float offset)
    {
        var point = track.SampleAt(s, out var tangent);
        return point + tangent.Cross(Vector3.Up).Normalized() * offset;
    }

    private int PlaceFenceLine(Vector3 from, Vector3 to)
    {
        var flatFrom = new Vector3(from.X, 0, from.Z);
        var flatTo = new Vector3(to.X, 0, to.Z);
        float length = flatFrom.DistanceTo(flatTo);
        if (length < 0.01f)
            return 0;

        int pieces = Mathf.Max(1, Mathf.RoundToInt(length / FenceLength));
        float pieceLength = length / pieces;
        var dir = (flatTo - flatFrom) / length;

        for (int k = 0; k < pieces; k++)
        {
            float t = FenceOriginCentered ? k + 0.5f : k;
            var pos = flatFrom + dir * (pieceLength * t);

            var node = Place(Pick(Fences), pos, dir, 1f, 0.3f, FenceYawOffset);
            if (AutoCollision)
                AddConvexCollision(node);
        }

        return pieces;
    }

    private int SpawnCityBorder()
    {
        if (!CityBorderBuildings || CityBuildings.Count == 0)
            return 0;

        int count = 0;
        float step = BuildingRadius * 2f + BuildingSpacing;
        float minRoad = _map.RoadWidth / 2f + _map.SidewalkWidth + BuildingSetback + BuildingRadius;
        var directions = new[]
        {
            Vector3.Right, Vector3.Left, Vector3.Forward, Vector3.Back,
            new Vector3(1, 0, 1).Normalized(), new Vector3(1, 0, -1).Normalized(),
            new Vector3(-1, 0, 1).Normalized(), new Vector3(-1, 0, -1).Normalized()
        };

        for (float x = step / 2f; x < _map.MapSize; x += step)
        {
            for (float z = step / 2f; z < _map.MapSize; z += step)
            {
                float jitter = step * 0.2f;
                var pos = new Vector3(x + _rng.RandfRange(-jitter, jitter), 0, z + _rng.RandfRange(-jitter, jitter));

                if (_map.Biomes.GetBiomeAt(pos) != Biome.City)
                    continue;

                float roadDist = _map.Track.DistanceToTrack(pos);
                if (roadDist < minRoad || roadDist > CityBorderReach)
                    continue;

                var towardForest = Vector3.Zero;
                foreach (var dir in directions)
                {
                    if (_map.Biomes.GetBiomeAt(pos + dir * CityBorderDepth) == Biome.Forest)
                        towardForest += dir;
                }

                if (towardForest == Vector3.Zero || !IsFree(pos, BuildingRadius))
                    continue;

                var node = Place(Pick(CityBuildings), pos, towardForest.Normalized(), 1f, BuildingRadius, ModelYawOffset);
                if (AutoCollision)
                    AddConvexCollision(node);
                count++;
            }
        }

        return count;
    }

    private int SpawnLamps()
    {
        if (StreetLamps.Count == 0)
            return 0;

        int count = 0;
        var track = _map.Track;
        float halfRoad = _map.RoadWidth / 2f;
        float probeOffset = halfRoad + _map.SidewalkWidth / 2f;

        for (int side = -1; side <= 1; side += 2)
        {
            if (side < 0 && !CityLampsBothSides)
                continue;

            float stagger = side > 0 ? CityLampSpacing / 2f : 0f;
            for (float s = stagger; s < track.Length; s += CityLampSpacing)
            {
                var center = track.SampleAt(s, out var tangent);
                var right = tangent.Cross(Vector3.Up).Normalized();

                if (!_map.HasSidewalkAt(center + right * probeOffset * side))
                    continue;

                var pos = center + right * (halfRoad + LampCurbDistance) * side;
                pos.Y += _map.SidewalkHeight;

                var node = Place(Pick(StreetLamps), pos, -right * side, 1f, 0.5f, LampYawOffset, false);
                if (AutoCollision)
                    AddCylinderCollision(node, LampPoleRadius, CollisionHeight);
                count++;
            }
        }

        return count;
    }

    private int Scatter(Godot.Collections.Array<PackedScene> scenes, float perKm2, float radius,
        Vector2 scaleRange, float collisionRadius, Func<Vector3, bool> zone)
    {
        if (scenes.Count == 0)
            return 0;

        int count = 0;
        int attempts = (int)(_map.MapSize * _map.MapSize / 1_000_000f * perKm2);

        for (int i = 0; i < attempts; i++)
        {
            var pos = RandomPosition();
            float scale = _rng.RandfRange(scaleRange.X, scaleRange.Y);
            float r = radius * scale;

            if (!zone(pos) || !IsFree(pos, r))
                continue;

            float yaw = _rng.Randf() * Mathf.Tau;
            var node = Place(Pick(scenes), pos, new Vector3(Mathf.Sin(yaw), 0, Mathf.Cos(yaw)), scale, r, 0f);
            if (AutoCollision)
                AddCylinderCollision(node, collisionRadius, CollisionHeight);
            count++;
        }

        return count;
    }

    private bool IsFree(Vector3 pos, float radius)
    {
        if (pos.X < 0 || pos.Z < 0 || pos.X > _map.MapSize || pos.Z > _map.MapSize)
            return false;

        if (_map.Mountain != null && _map.Mountain.Covers(pos, radius + 10f))
            return false;

        if (_map.Track.DistanceToTrack(pos) < _map.RoadWidth / 2f + RoadClearance + radius)
            return false;

        var p = new Vector2(pos.X, pos.Z);
        foreach (var placed in _placed)
        {
            if (p.DistanceTo(placed.pos) < radius + placed.radius)
                return false;
        }

        return true;
    }

    private Node3D Place(PackedScene scene, Vector3 pos, Vector3 facing, float scale, float radius,
        float yawOffsetDeg, bool snapToGround = true)
    {
        if (snapToGround)
            pos.Y = GroundBase(pos, radius);

        var node = scene.Instantiate<Node3D>();
        _container.AddChild(node);

        float yaw = Mathf.Atan2(facing.X, facing.Z) + Mathf.Pi + Mathf.DegToRad(yawOffsetDeg);
        node.Position = pos;
        node.Rotation = new Vector3(0, yaw, 0);
        node.Scale = Vector3.One * scale;

        _placed.Add((new Vector2(pos.X, pos.Z), radius));
        return node;
    }

    private float GroundBase(Vector3 pos, float radius)
    {
        float h = _map.GroundHeightAt(pos);
        if (radius < 2f)
            return h;

        float r = radius * 0.7f;
        h = Mathf.Min(h, _map.GroundHeightAt(pos + new Vector3(r, 0, 0)));
        h = Mathf.Min(h, _map.GroundHeightAt(pos + new Vector3(-r, 0, 0)));
        h = Mathf.Min(h, _map.GroundHeightAt(pos + new Vector3(0, 0, r)));
        h = Mathf.Min(h, _map.GroundHeightAt(pos + new Vector3(0, 0, -r)));
        return h;
    }

    private static void AddConvexCollision(Node3D node)
    {
        if (node is MeshInstance3D rootMesh)
            rootMesh.CreateConvexCollision();

        foreach (var child in node.FindChildren("*", "MeshInstance3D", true, false))
        {
            if (child is MeshInstance3D mesh)
                mesh.CreateConvexCollision();
        }
    }

    private static void AddCylinderCollision(Node3D node, float radius, float height)
    {
        var body = new StaticBody3D();
        body.AddChild(new CollisionShape3D()
        {
            Shape = new CylinderShape3D() { Radius = radius, Height = height },
            Position = new Vector3(0, height / 2f, 0)
        });
        node.AddChild(body);
    }

    private Vector3 RandomPosition() =>
        new(_rng.Randf() * _map.MapSize, 0, _rng.Randf() * _map.MapSize);

    private PackedScene Pick(Godot.Collections.Array<PackedScene> scenes) =>
        scenes[_rng.RandiRange(0, scenes.Count - 1)];
}