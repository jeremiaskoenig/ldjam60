using Godot;

public class BiomeMap
{
    public float Size { get; }

    private readonly Track _track;
    private readonly FastNoiseLite _noise;
    private readonly float _borderNoise;
    private readonly float _forestStart, _cityStart, _desertStart;
    private readonly float _townStart, _townEnd, _townDepth;

    public BiomeMap(Track track, float size, float forestStart, float cityStart, float desertStart,
        float townStart, float townEnd, float townDepth, float borderNoise, RandomNumberGenerator rng)
    {
        _track = track;
        Size = size;
        _forestStart = forestStart;
        _cityStart = cityStart;
        _desertStart = desertStart;
        _townStart = townStart;
        _townEnd = townEnd;
        _townDepth = townDepth;
        _borderNoise = borderNoise;
        _noise = new FastNoiseLite()
        {
            Seed = (int)rng.Randi(),
            Frequency = 1f / 300f
        };
    }

    public Biome BiomeAtProgress(float p)
    {
        if (p >= _forestStart && p < _cityStart)
            return Biome.Forest;
        if (p >= _cityStart && p < _desertStart)
            return Biome.City;
        return Biome.Desert;
    }

    public Biome GetBiomeAt(Vector3 position)
    {
        float warpX = _noise.GetNoise2D(position.X, position.Z) * _borderNoise;
        float warpZ = _noise.GetNoise2D(position.X + 5000f, position.Z - 5000f) * _borderNoise;
        var warped = new Vector3(position.X + warpX, 0, position.Z + warpZ);

        int index = _track.ClosestIndex(warped, out _);
        return BiomeAtProgress(_track.ProgressOf(index));
    }

    public bool IsInTown(Vector3 position)
    {
        int index = _track.ClosestIndex(position, out float distance);
        float p = _track.ProgressOf(index);
        return distance < _townDepth && p >= _townStart && p <= _townEnd;
    }

    public ImageTexture CreateTexture(int resolution)
    {
        var image = Image.CreateEmpty(resolution, resolution, false, Image.Format.Rgb8);

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                var world = new Vector3((x + 0.5f) / resolution * Size, 0, (y + 0.5f) / resolution * Size);
                image.SetPixel(x, y, GetBiomeAt(world).MaskColor());
            }
        }

        return ImageTexture.CreateFromImage(image);
    }
}