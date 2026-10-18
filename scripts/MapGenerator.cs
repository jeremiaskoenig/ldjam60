using Godot;

public partial class MapGenerator : Node3D
{
    [Export] public int GridSize { get; set; } = 16;
    [Export] public float CellSize { get; set; } = 250;
    [Export] public ulong Seed { get; set; }
    [Export(PropertyHint.Range, "0,1,0.05")] public float ExtraEdgeChance { get; set; } = 0.2f;
    [Export(PropertyHint.Range, "0,1,0.05")] public float DiagonalChance { get; set; } = 0.25f;

    [ExportGroup("Straßen")]
    [Export] public float RoadWidth { get; set; } = 14f;
    [Export] public bool ShowDebug { get; set; } = false;

    [ExportGroup("Materialien")]
    [Export] public Material GroundMaterial { get; set; }
    [Export] public Material RoadMaterial { get; set; }

    [ExportGroup("Markierungen")]
    [Export] public bool ShowMarkings { get; set; } = true;
    [Export] public float LineWidth { get; set; } = 0.15f;
    [Export] public float EdgeLineInset { get; set; } = 0.5f;
    [Export] public float DashLength { get; set; } = 3f;
    [Export] public float DashGap { get; set; } = 6f;
    [Export] public Material MarkingMaterial { get; set; }

    [ExportGroup("Biome")]
    [Export] public int BiomeSeedsPerType { get; set; } = 2;
    [Export] public float BiomeBorderNoise { get; set; } = 300f;
    [Export] public int BiomeMapResolution { get; set; } = 128;

    [ExportGroup("Objekte")]
    [Export] public PropSpawner Props { get; set; }

    [ExportGroup("Gehwege")]
    [Export] public float SidewalkWidth { get; set; } = 3f;
    [Export] public float SidewalkHeight { get; set; } = 0.12f;
    [Export] public bool SidewalkCollision { get; set; } = true;
    [Export] public Material SidewalkMaterial { get; set; }

    private const float RoadHeight = 0.05f;
    private const float MarkingHeight = 0.08f;

    public RoadNetwork Network { get; private set; }
    public BiomeMap Biomes { get; private set; }
    public float MapSize => GridSize * CellSize;

    private readonly RandomNumberGenerator _rng = new();

    public override void _Ready()
    {
        _rng.Seed = (ulong)GD.Hash(Seed);
        GD.Print($"Seed: {Seed}");

        Network = new RoadNetwork(GridSize, CellSize, _rng);
        Network.Generate(DiagonalChance, ExtraEdgeChance);

        Biomes = new BiomeMap(MapSize, BiomeSeedsPerType, BiomeBorderNoise, _rng);

        GD.Print($"{Network.Points.Count} Punkte, {Network.CandidateCount} mögliche Kanten, {Network.Edges.Count} Straßen");

        BuildGround();
        BuildRoads();
        BuildSidewalks();

        if (ShowMarkings)
            BuildMarkings();

        if (ShowDebug)
        {
            AddChild(DebugDraw.CreateNodeMarkers(Network, Biomes));
            AddChild(DebugDraw.CreateEdgeLines(Network));
        }

        Props?.Spawn(this);
    }

    private void BuildGround()
    {
        if (GroundMaterial is ShaderMaterial groundShader)
        {
            groundShader.SetShaderParameter("biome_map", Biomes.CreateTexture(BiomeMapResolution));
            groundShader.SetShaderParameter("map_size", MapSize);
        }

        AddChild(new MeshInstance3D()
        {
            Name = "Ground",
            Mesh = new PlaneMesh() { Size = new Vector2(MapSize + 400, MapSize + 400) },
            MaterialOverride = GroundMaterial ?? new StandardMaterial3D() { AlbedoColor = new Color(0.3f, 0.5f, 0.25f) },
            Position = new Vector3(MapSize / 2f, 0, MapSize / 2f)
        });

        var body = new StaticBody3D() { Name = "GroundCollision" };
        body.AddChild(new CollisionShape3D() { Shape = new WorldBoundaryShape3D() });
        AddChild(body);
    }

    private void BuildRoads()
    {
        var roads = new MeshInstance3D()
        {
            Name = "Roads",
            Mesh = RoadMeshBuilder.BuildRoads(Network, RoadWidth, RoadHeight),
            MaterialOverride = RoadMaterial ?? new StandardMaterial3D()
            {
                AlbedoColor = new Color(0.2f, 0.2f, 0.22f),
                Roughness = 0.9f
            }
        };
        AddChild(roads);
        roads.CreateTrimeshCollision();
    }

    private void BuildMarkings()
    {
        AddChild(new MeshInstance3D()
        {
            Name = "Markings",
            Mesh = RoadMeshBuilder.BuildMarkings(Network, RoadWidth, LineWidth,
                EdgeLineInset, DashLength, DashGap, MarkingHeight),
            MaterialOverride = MarkingMaterial ?? new StandardMaterial3D()
            {
                AlbedoColor = new Color(0.9f, 0.9f, 0.85f),
                Roughness = 0.5f
            }
        });
    }

    private void BuildSidewalks()
    {
        var mesh = RoadMeshBuilder.BuildSidewalks(Network, Biomes, RoadWidth, SidewalkWidth, SidewalkHeight);
        if (mesh == null)
            return;

        var sidewalks = new MeshInstance3D()
        {
            Name = "Sidewalks",
            Mesh = mesh,
            MaterialOverride = SidewalkMaterial ?? new StandardMaterial3D()
            {
                AlbedoColor = new Color(0.6f, 0.6f, 0.58f),
                Roughness = 0.85f
            }
        };
        AddChild(sidewalks);

        if (SidewalkCollision)
            sidewalks.CreateTrimeshCollision();
    }
}