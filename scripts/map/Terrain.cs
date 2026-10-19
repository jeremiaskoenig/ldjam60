using Godot;

public class Terrain
{
    private readonly Track _track;
    private readonly Mountain _mountain;
    private readonly FastNoiseLite _hillNoise;
    private readonly FastNoiseLite _elevationNoise;
    private readonly float _size;
    private readonly float _amplitude;
    private readonly float _flatDistance;
    private readonly float _ramp;
    private readonly float _elevationAmplitude;
    private readonly float[,] _factor;
    private readonly float _tunnelBlend;
    private readonly float _mountainBaseY;
    private const float TunnelFlatPad = 30f;

    private const int FactorResolution = 64;
    private const float RoadBlendStart = 12f;
    private const float RoadBlendEnd = 35f;

    public Terrain(Track track, BiomeMap biomes, Mountain mountain, float mapSize,
        float amplitude, float frequency, float flatDistance, float ramp,
        float desertFactor, float forestFactor, float cityFactor,
        float elevationAmplitude, float elevationFrequency, float tunnelBlend, int seed)
    {
        _track = track;
        _mountain = mountain;
        _size = mapSize;
        _amplitude = amplitude;
        _flatDistance = flatDistance;
        _ramp = ramp;
        _elevationAmplitude = elevationAmplitude;

        _hillNoise = new FastNoiseLite() { Seed = seed, Frequency = frequency };
        _elevationNoise = new FastNoiseLite()
        {
            Seed = seed + 1,
            Frequency = elevationFrequency,
            FractalType = FastNoiseLite.FractalTypeEnum.None
        };

        _tunnelBlend = tunnelBlend;
        _mountainBaseY = mountain != null ? RawElevation(mountain.Center) : 0f;

        int res = FactorResolution;
        var raw = new float[res + 1, res + 1];
        for (int x = 0; x <= res; x++)
        {
            for (int z = 0; z <= res; z++)
            {
                var p = new Vector3((float)x / res * mapSize, 0, (float)z / res * mapSize);
                float f = biomes.GetBiomeAt(p) switch
                {
                    Biome.Desert => desertFactor,
                    Biome.City => cityFactor,
                    _ => forestFactor
                };

                if (biomes.IsInTown(p))
                    f = 0f;
                if (mountain != null && mountain.Covers(p, 60f))
                    f = 0f;

                raw[x, z] = f;
            }
        }

        _factor = Blur(raw, 3);
    }

    public float ElevationAt(Vector3 p)
    {
        if (_elevationAmplitude <= 0f)
            return 0f;

        float raw = RawElevation(p);
        if (_mountain == null)
            return raw;

        float d = _mountain.DistanceToCore(p);
        float t = Mathf.Clamp((d - TunnelFlatPad) / _tunnelBlend, 0f, 1f);
        return Mathf.Lerp(_mountainBaseY, raw, Smooth(t));
    }

    private float RawElevation(Vector3 p) =>
        (_elevationNoise.GetNoise2D(p.X, p.Z) * 0.5f + 0.5f) * _elevationAmplitude;

    public float HeightAt(Vector3 p)
    {
        float d = _track.DistanceToTrack(p, out float roadY);

        float blend = Smooth(Mathf.Clamp((d - RoadBlendStart) / (RoadBlendEnd - RoadBlendStart), 0f, 1f));
        float baseHeight = Mathf.Lerp(roadY, ElevationAt(p), blend);

        if (_amplitude <= 0f)
            return baseHeight;

        float factor = SampleFactor(p);
        if (factor <= 0.001f)
            return baseHeight;

        float mask = Mathf.Clamp((d - _flatDistance) / _ramp, 0f, 1f);
        if (mask <= 0f)
            return baseHeight;

        float n = _hillNoise.GetNoise2D(p.X, p.Z) * 0.5f + 0.5f;
        return baseHeight + n * _amplitude * factor * Smooth(mask);
    }

    public ArrayMesh BuildMesh(float cellSize, float margin)
    {
        float start = -margin;
        int cells = Mathf.CeilToInt((_size + 2f * margin) / cellSize);

        var h = new float[cells + 1, cells + 1];
        for (int x = 0; x <= cells; x++)
            for (int z = 0; z <= cells; z++)
                h[x, z] = HeightAt(new Vector3(start + x * cellSize, 0, start + z * cellSize));

        var st = MeshUtil.Begin();
        for (int x = 0; x < cells; x++)
        {
            for (int z = 0; z < cells; z++)
            {
                float x0 = start + x * cellSize, x1 = x0 + cellSize;
                float z0 = start + z * cellSize, z1 = z0 + cellSize;

                MeshUtil.AddQuad(st,
                    new Vector3(x0, h[x, z], z0), new Vector3(x1, h[x + 1, z], z0),
                    new Vector3(x1, h[x + 1, z + 1], z1), new Vector3(x0, h[x, z + 1], z1),
                    Normal(x, z), Normal(x + 1, z), Normal(x + 1, z + 1), Normal(x, z + 1));
            }
        }

        st.Index();
        return st.Commit();

        Vector3 Normal(int x, int z)
        {
            float hl = h[Mathf.Max(x - 1, 0), z];
            float hr = h[Mathf.Min(x + 1, cells), z];
            float hd = h[x, Mathf.Max(z - 1, 0)];
            float hu = h[x, Mathf.Min(z + 1, cells)];
            return new Vector3(hl - hr, 2f * cellSize, hd - hu).Normalized();
        }
    }

    private static float Smooth(float x) => x * x * (3f - 2f * x);

    private float SampleFactor(Vector3 p)
    {
        int res = FactorResolution;
        float fx = Mathf.Clamp(p.X / _size * res, 0f, res);
        float fz = Mathf.Clamp(p.Z / _size * res, 0f, res);
        int x0 = Mathf.Min((int)fx, res - 1);
        int z0 = Mathf.Min((int)fz, res - 1);
        float tx = fx - x0;
        float tz = fz - z0;

        float a = Mathf.Lerp(_factor[x0, z0], _factor[x0 + 1, z0], tx);
        float b = Mathf.Lerp(_factor[x0, z0 + 1], _factor[x0 + 1, z0 + 1], tx);
        return Mathf.Lerp(a, b, tz);
    }

    private static float[,] Blur(float[,] source, int passes)
    {
        int w = source.GetLength(0);
        int d = source.GetLength(1);
        var current = source;

        for (int pass = 0; pass < passes; pass++)
        {
            var next = new float[w, d];
            for (int x = 0; x < w; x++)
            {
                for (int z = 0; z < d; z++)
                {
                    float sum = 0f;
                    for (int dx = -1; dx <= 1; dx++)
                        for (int dz = -1; dz <= 1; dz++)
                            sum += current[Mathf.Clamp(x + dx, 0, w - 1), Mathf.Clamp(z + dz, 0, d - 1)];
                    next[x, z] = sum / 9f;
                }
            }
            current = next;
        }

        return current;
    }
}