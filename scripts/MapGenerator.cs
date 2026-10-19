using Godot;

public partial class MapGenerator : Node3D
{
    [Export] public ulong Seed { get; set; }
    [Export] public float MapSize { get; set; } = 1600f;

    [ExportGroup("Strecke")]
    [Export] public int ControlPoints { get; set; } = 10;
    [Export(PropertyHint.Range, "0.2,1,0.05")] public float TrackMinRadius { get; set; } = 0.5f;
    [Export(PropertyHint.Range, "0.2,1,0.05")] public float TrackMaxRadius { get; set; } = 0.85f;
    [Export] public float SampleSpacing { get; set; } = 8f;
    [Export] public float TrackMinClearance { get; set; } = 80f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float StartProgress { get; set; } = 0.06f;

    [ExportGroup("Straße")]
    [Export] public float RoadWidth { get; set; } = 14f;
    [Export] public Material RoadMaterial { get; set; }
    [Export] public Material GroundMaterial { get; set; }
    [Export] public Material StartLineMaterial { get; set; }

    [ExportGroup("Markierungen")]
    [Export] public bool ShowMarkings { get; set; } = true;
    [Export] public float LineWidth { get; set; } = 0.15f;
    [Export] public float EdgeLineInset { get; set; } = 0.5f;
    [Export] public float DashLength { get; set; } = 3f;
    [Export] public float DashGap { get; set; } = 6f;
    [Export] public Material MarkingMaterial { get; set; }

    [ExportGroup("Biome")]
    [Export(PropertyHint.Range, "0,1,0.01")] public float ForestStart { get; set; } = 0.33f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float CityStart { get; set; } = 0.62f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float DesertStart { get; set; } = 0.92f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float TownStart { get; set; } = 0.02f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float TownEnd { get; set; } = 0.14f;
    [Export] public float TownDepth { get; set; } = 80f;
    [Export] public float BiomeBorderNoise { get; set; } = 60f;
    [Export] public int BiomeMapResolution { get; set; } = 128;

    [ExportGroup("Berg")]
    [Export] public bool BuildMountain { get; set; } = true;
    [Export(PropertyHint.Range, "0,1,0.005")] public float MountainProgress { get; set; } = 0.955f;
    [Export] public float MountainAlongRadius { get; set; } = 50f;
    [Export] public float MountainAcrossRadius { get; set; } = 300f;
    [Export] public float MountainInnerRadius { get; set; } = 500f;
    [Export] public float MountainHeight { get; set; } = 70f;
    [Export] public float MountainCellSize { get; set; } = 5f;
    [Export] public Material MountainMaterial { get; set; }
    [Export] public PackedScene TunnelScene { get; set; }
    [Export] public float TunnelLength { get; set; } = 54f;
    [Export] public float TunnelCenterOffset { get; set; } = 0f;
    [Export] public float TunnelScale { get; set; } = 1f;
    [Export] public float TunnelHalfWidth { get; set; } = 7f;
    [Export] public float TunnelHeight { get; set; } = 10f;
    [Export] public int TunnelCount { get; set; } = 5;
    [Export] public float PortalCover { get; set; } = 4f;

    [ExportGroup("Hügel")]
    [Export] public bool BuildHills { get; set; } = true;
    [Export] public float HillHeight { get; set; } = 25f;
    [Export] public float HillFrequency { get; set; } = 0.004f;
    [Export] public float HillFlatDistance { get; set; } = 45f;
    [Export] public float HillRamp { get; set; } = 80f;
    [Export(PropertyHint.Range, "0,1,0.05")] public float DesertHills { get; set; } = 0.6f;
    [Export(PropertyHint.Range, "0,1,0.05")] public float ForestHills { get; set; } = 1f;
    [Export(PropertyHint.Range, "0,1,0.05")] public float CityHills { get; set; } = 0.15f;
    [Export] public float RoadElevation { get; set; } = 15f;
    [Export] public float RoadElevationFrequency { get; set; } = 0.0012f;
    [Export] public float TerrainCellSize { get; set; } = 8f;
    [Export] public float TunnelElevationBlend { get; set; } = 300f;

    [ExportGroup("Gehwege")]
    [Export] public float SidewalkWidth { get; set; } = 3f;
    [Export] public float SidewalkHeight { get; set; } = 0.12f;
    [Export] public bool SidewalkCollision { get; set; } = true;
    [Export] public Material SidewalkMaterial { get; set; }

    [ExportGroup("Begrenzung")]
    [Export] public bool BuildBoundaryWalls { get; set; } = false;
    [Export] public float BoundaryMargin { get; set; } = 40f;
    [Export] public float BoundaryHeight { get; set; } = 30f;

    [ExportGroup("Objekte")]
    [Export] public PropSpawner Props { get; set; }

    private const float RoadHeight = 0.05f;
    private const float MarkingHeight = 0.08f;
    private const float StartLineHeight = 0.1f;

    public Track Track { get; private set; }
    public BiomeMap Biomes { get; private set; }
    public Mountain Mountain { get; private set; }
    public Terrain Terrain { get; private set; }
    public Transform3D StartTransform { get; private set; }

    private readonly RandomNumberGenerator _rng = new();

    public override void _Ready()
    {
        _rng.Seed = (ulong)GD.Hash(Seed);

        Track = Track.Generate(_rng, MapSize, ControlPoints, TrackMinRadius, TrackMaxRadius, SampleSpacing, TrackMinClearance);

        int mountainIndex = Track.IndexAtProgress(MountainProgress);
        float coreHalf = TunnelLength * TunnelScale * TunnelCount / 2f;
        if (BuildMountain)
            Track.Straighten(mountainIndex, coreHalf + MountainAlongRadius, 80f);

        Biomes = new BiomeMap(Track, MapSize, ForestStart, CityStart, DesertStart,
            TownStart, TownEnd, TownDepth, BiomeBorderNoise, _rng);

        if (BuildMountain)
            Mountain = new Mountain(Track, mountainIndex, coreHalf, MountainAlongRadius,
                MountainAcrossRadius, MountainInnerRadius, new Vector3(MapSize / 2f, 0, MapSize / 2f),
                MountainHeight, TunnelHeight * TunnelScale + PortalCover, (int)_rng.Randi());

        Terrain = new Terrain(Track, Biomes, Mountain, MapSize,
            BuildHills ? HillHeight : 0f, HillFrequency, HillFlatDistance, HillRamp,
            DesertHills, ForestHills, CityHills,
            RoadElevation, RoadElevationFrequency, TunnelElevationBlend, (int)_rng.Randi());

        Track.ApplyElevation(Terrain.ElevationAt);

        if (Mountain != null)
        {
            Mountain.BaseY = Terrain.ElevationAt(Mountain.Center);
            Mountain.BaseHeight = Terrain.HeightAt;
        }

        var startPos = Track.SampleAt(StartProgress * Track.Length, out var startDir) + Vector3.Up;
        StartTransform = new Transform3D(Basis.Identity, startPos).LookingAt(startPos + startDir, Vector3.Up);

        GD.Print($"Seed {Seed}: Strecke {Track.Length:0} m mit {Track.Count} Punkten");

        BuildGround();
        if (BuildBoundaryWalls)
            BuildBoundary();
        BuildRoad();
        BuildSidewalks();
        if (ShowMarkings)
            BuildMarkings();
        BuildStartLine();
        if (Mountain != null)
            BuildMountainMeshes();

        Props?.Spawn(this);
    }

    public float ProgressAt(Vector3 position) => Track.ProgressOf(Track.ClosestIndex(position, out _));

    public bool HasSidewalkAt(Vector3 probe) =>
        Biomes.GetBiomeAt(probe) == Biome.City && (Mountain == null || !Mountain.Covers(probe, 10f));

    public float GroundHeightAt(Vector3 position) => Terrain?.HeightAt(position) ?? 0f;

    private void BuildGround()
    {
        if (GroundMaterial is ShaderMaterial groundShader)
        {
            groundShader.SetShaderParameter("biome_map", Biomes.CreateTexture(BiomeMapResolution));
            groundShader.SetShaderParameter("map_size", MapSize);
        }

        var material = GroundMaterial ?? new StandardMaterial3D() { AlbedoColor = new Color(0.3f, 0.5f, 0.25f) };
        float size = MapSize + 400f;
        var center = new Vector3(MapSize / 2f, 0, MapSize / 2f);

        var ground = new MeshInstance3D()
        {
            Name = "Ground",
            Mesh = Terrain.BuildMesh(TerrainCellSize, 200f),
            MaterialOverride = material
        };
        AddChild(ground);
        ground.CreateTrimeshCollision();

        var body = new StaticBody3D() { Name = "GroundCollision", Position = center + Vector3.Down };
        body.AddChild(new CollisionShape3D() { Shape = new BoxShape3D() { Size = new Vector3(size, 2f, size) } });
        AddChild(body);
    }

    private void BuildBoundary()
    {
        var body = new StaticBody3D() { Name = "Boundary" };
        float min = -BoundaryMargin;
        float max = MapSize + BoundaryMargin;
        float length = max - min + 4f;
        float mid = MapSize / 2f;
        float y = BoundaryHeight / 2f;

        AddBoundaryWall(body, new Vector3(min, y, mid), new Vector3(2f, BoundaryHeight, length));
        AddBoundaryWall(body, new Vector3(max, y, mid), new Vector3(2f, BoundaryHeight, length));
        AddBoundaryWall(body, new Vector3(mid, y, min), new Vector3(length, BoundaryHeight, 2f));
        AddBoundaryWall(body, new Vector3(mid, y, max), new Vector3(length, BoundaryHeight, 2f));

        AddChild(body);
    }

    private static void AddBoundaryWall(StaticBody3D body, Vector3 position, Vector3 size)
    {
        body.AddChild(new CollisionShape3D()
        {
            Shape = new BoxShape3D() { Size = size },
            Position = position
        });
    }

    private void BuildRoad()
    {
        var road = new MeshInstance3D()
        {
            Name = "Road",
            Mesh = TrackMeshBuilder.BuildRoad(Track, RoadWidth, RoadHeight),
            MaterialOverride = RoadMaterial ?? new StandardMaterial3D()
            {
                AlbedoColor = new Color(0.2f, 0.2f, 0.22f),
                Roughness = 0.9f
            }
        };
        AddChild(road);
        road.CreateTrimeshCollision();
    }

    private void BuildSidewalks()
    {
        var mesh = TrackMeshBuilder.BuildSidewalks(Track, HasSidewalkAt, RoadWidth, SidewalkWidth, SidewalkHeight);
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

    private void BuildMarkings()
    {
        AddChild(new MeshInstance3D()
        {
            Name = "Markings",
            Mesh = TrackMeshBuilder.BuildMarkings(Track, RoadWidth, LineWidth, EdgeLineInset, DashLength, DashGap, MarkingHeight),
            MaterialOverride = MarkingMaterial ?? new StandardMaterial3D()
            {
                AlbedoColor = new Color(0.9f, 0.9f, 0.85f),
                Roughness = 0.5f
            }
        });
    }

    private void BuildStartLine()
    {
        AddChild(new MeshInstance3D()
        {
            Name = "StartLine",
            Mesh = TrackMeshBuilder.BuildStartLine(Track, StartProgress, RoadWidth, 10, StartLineHeight),
            MaterialOverride = StartLineMaterial ?? new StandardMaterial3D()
            {
                VertexColorUseAsAlbedo = true,
                Roughness = 0.6f
            }
        });
    }

    private void BuildMountainMeshes()
    {
        float corridorHalf = TunnelHalfWidth * TunnelScale + 2f;
        var rock = MountainMaterial ?? new StandardMaterial3D()
        {
            AlbedoColor = new Color(0.45f, 0.38f, 0.3f),
            Roughness = 1f
        };

        var terrain = new MeshInstance3D()
        {
            Name = "Mountain",
            Mesh = Mountain.BuildTerrain(MountainCellSize, corridorHalf),
            MaterialOverride = rock
        };
        AddChild(terrain);
        terrain.CreateTrimeshCollision();

        var cut = new MeshInstance3D()
        {
            Name = "MountainCut",
            Mesh = Mountain.BuildCut(corridorHalf, TunnelHalfWidth * TunnelScale, TunnelHeight * TunnelScale, MountainCellSize),
            MaterialOverride = rock
        };
        AddChild(cut);
        AddDoubleSidedCollision(cut);

        if (TunnelScene == null)
            return;

        float segment = TunnelLength * TunnelScale;
        for (int k = 0; k < TunnelCount; k++)
        {
            float shift = (k - (TunnelCount - 1) / 2f) * segment;

            var tunnel = TunnelScene.Instantiate<Node3D>();
            tunnel.Name = $"Tunnel{k}";
            AddChild(tunnel);
            tunnel.Transform = Mountain.TunnelTransform(TunnelCenterOffset, TunnelScale, shift);

            foreach (var child in tunnel.FindChildren("*", "MeshInstance3D", true, false))
            {
                if (child is MeshInstance3D mesh)
                    AddDoubleSidedCollision(mesh);
            }
        }
    }

    private static void AddDoubleSidedCollision(MeshInstance3D meshInstance)
    {
        var shape = meshInstance.Mesh.CreateTrimeshShape();
        shape.BackfaceCollision = true;

        var body = new StaticBody3D();
        body.AddChild(new CollisionShape3D() { Shape = shape });
        meshInstance.AddChild(body);
    }
}