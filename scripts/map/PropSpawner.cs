using Godot;
using System.Collections.Generic;

public partial class PropSpawner : Node3D
{
    [ExportGroup("Stadt")]
    [Export] public Godot.Collections.Array<PackedScene> CityBuildings { get; set; } = new();
    [Export] public float BuildingRadius { get; set; } = 10f;
    [Export] public float BuildingSetback { get; set; } = 4f;
    [Export] public float BuildingSpacing { get; set; } = 3f;
    [Export] public int BuildingRows { get; set; } = 2;

    [ExportGroup("Wüste")]
    [Export] public Godot.Collections.Array<PackedScene> Farmhouses { get; set; } = new();
    [Export] public float FarmRadius { get; set; } = 15f;
    [Export] public float FarmMinDistance { get; set; } = 300f;
    [Export] public float FarmMaxRoadDistance { get; set; } = 80f;

    [ExportGroup("Wald")]
    [Export] public Godot.Collections.Array<PackedScene> Trees { get; set; } = new();
    [Export] public float TreeRadius { get; set; } = 3f;
    [Export] public float TreesPerKm2 { get; set; } = 400f;
    [Export] public Vector2 TreeScaleRange { get; set; } = new(0.8f, 1.3f);

    [ExportGroup("Allgemein")]
    [Export] public float RoadClearance { get; set; } = 3f;
    [Export(PropertyHint.Range, "-180,180,90")] public float ModelYawOffset { get; set; } = 0f;

    [ExportGroup("Laternen")]
    [Export] public Godot.Collections.Array<PackedScene> StreetLamps { get; set; } = new();
    [Export] public float CityLampSpacing { get; set; } = 50f;
    [Export] public float RuralLampSpacing { get; set; } = 100f;
    [Export] public float LampCurbDistance { get; set; } = 0.6f;
    [Export] public float RuralLampDistance { get; set; } = 1.5f;
    [Export(PropertyHint.Range, "-180,180,90")] public float LampYawOffset { get; set; } = 0f;

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

        int buildings = SpawnCityBuildings();
        int lamps = SpawnStreetLamps();
        int farms = SpawnFarmhouses();
        int trees = SpawnTrees();

        GD.Print($"Props: {buildings} Gebäude, {lamps} Laternen, {farms} Farmen, {trees} Bäume");
    }

    private int SpawnCityBuildings()
    {
        if (CityBuildings.Count == 0)
            return 0;

        int count = 0;
        var points = _map.Network.Points;
        float step = BuildingRadius * 2f + BuildingSpacing;

        foreach (var edge in _map.Network.Edges)
        {
            var a = points[edge.a];
            var b = points[edge.b];
            var dir = (b - a).Normalized();
            var right = dir.Cross(Vector3.Up);
            float length = a.DistanceTo(b);

            for (int row = 0; row < BuildingRows; row++)
            {
                float offset = _map.RoadWidth / 2f + _map.SidewalkWidth + BuildingSetback + BuildingRadius + row * step;

                for (float d = step / 2f; d < length; d += step)
                {
                    for (int side = -1; side <= 1; side += 2)
                    {
                        var pos = a + dir * d + right * offset * side;

                        if (_map.Biomes.GetBiomeAt(pos) != Biome.City || !IsFree(pos, BuildingRadius))
                            continue;

                        var towardRoad = -right * side;
                        Place(Pick(CityBuildings), pos, towardRoad, 1f, BuildingRadius, ModelYawOffset);
                        count++;
                    }
                }
            }
        }

        return count;
    }

    private int SpawnFarmhouses()
    {
        if (Farmhouses.Count == 0)
            return 0;

        int count = 0;
        var farmPositions = new List<Vector3>();
        int attempts = (int)(_map.MapSize * _map.MapSize / 1_000_000f * 50f);

        for (int i = 0; i < attempts; i++)
        {
            var pos = RandomPosition();

            if (_map.Biomes.GetBiomeAt(pos) != Biome.Desert || !IsFree(pos, FarmRadius))
                continue;

            var roadPoint = _map.Network.ClosestPointOnRoad(pos, out float roadDist);
            if (roadDist > FarmMaxRoadDistance)
                continue;

            bool tooClose = false;
            foreach (var other in farmPositions)
            {
                if (other.DistanceTo(pos) < FarmMinDistance)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose)
                continue;

            Place(Pick(Farmhouses), pos, (roadPoint - pos).Normalized(), 1f, FarmRadius, ModelYawOffset);
            farmPositions.Add(pos);
            count++;
        }

        return count;
    }

    private int SpawnTrees()
    {
        if (Trees.Count == 0)
            return 0;

        int count = 0;
        int attempts = (int)(_map.MapSize * _map.MapSize / 1_000_000f * TreesPerKm2);

        for (int i = 0; i < attempts; i++)
        {
            var pos = RandomPosition();
            float scale = _rng.RandfRange(TreeScaleRange.X, TreeScaleRange.Y);
            float radius = TreeRadius * scale;

            if (_map.Biomes.GetBiomeAt(pos) != Biome.Forest || !IsFree(pos, radius))
                continue;

            float yaw = _rng.Randf() * Mathf.Tau;
            Place(Pick(Trees), pos, new Vector3(Mathf.Sin(yaw), 0, Mathf.Cos(yaw)), scale, radius, 0f);
            count++;
        }

        return count;
    }

    private bool IsFree(Vector3 pos, float radius)
    {
        if (pos.X < 0 || pos.Z < 0 || pos.X > _map.MapSize || pos.Z > _map.MapSize)
            return false;

        _map.Network.ClosestPointOnRoad(pos, out float roadDist);
        if (roadDist < _map.RoadWidth / 2f + RoadClearance + radius)
            return false;

        var p = new Vector2(pos.X, pos.Z);
        foreach (var placed in _placed)
        {
            if (p.DistanceTo(placed.pos) < radius + placed.radius)
                return false;
        }

        return true;
    }

    private void Place(PackedScene scene, Vector3 pos, Vector3 facing, float scale, float radius, float yawOffsetDeg)
    {
        var node = scene.Instantiate<Node3D>();
        _container.AddChild(node);

        float yaw = Mathf.Atan2(facing.X, facing.Z) + Mathf.Pi + Mathf.DegToRad(yawOffsetDeg);

        node.Position = pos;
        node.Rotation = new Vector3(0, yaw, 0);
        node.Scale = Vector3.One * scale;

        _placed.Add((new Vector2(pos.X, pos.Z), radius));
    }

    private Vector3 RandomPosition() =>
        new(_rng.Randf() * _map.MapSize, 0, _rng.Randf() * _map.MapSize);

    private PackedScene Pick(Godot.Collections.Array<PackedScene> scenes) =>
        scenes[_rng.RandiRange(0, scenes.Count - 1)];

    private int SpawnStreetLamps()
    {
        if (StreetLamps.Count == 0)
            return 0;

        int count = 0;
        var points = _map.Network.Points;
        float halfRoad = _map.RoadWidth / 2f;
        float gap = _map.RoadWidth + _map.SidewalkWidth;
        int ruralEvery = RuralLampSpacing <= 0f ? 0 : Mathf.Max(1, Mathf.RoundToInt(RuralLampSpacing / CityLampSpacing));

        foreach (var edge in _map.Network.Edges)
        {
            var a = points[edge.a];
            var b = points[edge.b];
            var dir = (b - a).Normalized();
            var right = dir.Cross(Vector3.Up);
            float length = a.DistanceTo(b);

            if (length <= gap * 2f)
                continue;

            for (int side = -1; side <= 1; side += 2)
            {
                float stagger = side > 0 ? CityLampSpacing / 2f : 0f;
                int index = 0;

                for (float d = gap + stagger; d < length - gap; d += CityLampSpacing, index++)
                {
                    var basePos = a + dir * d;
                    var probe = basePos + right * (halfRoad + _map.SidewalkWidth / 2f) * side;
                    bool inCity = _map.Biomes.GetBiomeAt(probe) == Biome.City;

                    if (!inCity && (side < 0 || ruralEvery == 0 || index % ruralEvery != 0))
                        continue;

                    float offset = halfRoad + (inCity ? LampCurbDistance : RuralLampDistance);
                    var pos = basePos + right * offset * side;
                    if (inCity)
                        pos.Y = _map.SidewalkHeight;

                    _map.Network.ClosestPointOnRoad(pos, out float roadDist);
                    if (roadDist < offset - 0.5f)
                        continue;

                    Place(Pick(StreetLamps), pos, -right * side, 1f, 0.5f, LampYawOffset);
                    count++;
                }
            }
        }

        return count;
    }
}