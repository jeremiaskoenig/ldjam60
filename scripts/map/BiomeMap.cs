using Godot;
using System;
using System.Collections.Generic;

public class BiomeMap
{
    public float Size { get; }

    private readonly List<(Vector3 pos, Biome biome)> _seeds = new();
    private readonly FastNoiseLite _noise;
    private readonly float _borderNoise;

    public BiomeMap(float size, int seedsPerType, float borderNoise, RandomNumberGenerator rng)
    {
        Size = size;
        _borderNoise = borderNoise;
        _noise = new FastNoiseLite()
        {
            Seed = (int)rng.Randi(),
            Frequency = 1f / 800f
        };

        PlaceSeeds(seedsPerType, rng);
    }

    private void PlaceSeeds(int seedsPerType, RandomNumberGenerator rng)
    {
        int total = seedsPerType * 3;
        float minDist = Size / Mathf.Sqrt(total) * 0.6f;

        for (int i = 0; i < seedsPerType; i++)
        {
            foreach (var biome in Enum.GetValues<Biome>())
            {
                var candidate = Vector3.Zero;
                for (int attempt = 0; attempt < 30; attempt++)
                {
                    candidate = new Vector3(rng.Randf() * Size, 0, rng.Randf() * Size);
                    if (!IsTooClose(candidate, minDist))
                        break;
                }
                _seeds.Add((candidate, biome));
            }
        }
    }

    private bool IsTooClose(Vector3 candidate, float minDist)
    {
        foreach (var seed in _seeds)
        {
            if (seed.pos.DistanceTo(candidate) < minDist)
                return true;
        }
        return false;
    }

    public Biome GetBiomeAt(Vector3 position)
    {
        float warpX = _noise.GetNoise2D(position.X, position.Z) * _borderNoise;
        float warpZ = _noise.GetNoise2D(position.X + 5000f, position.Z - 5000f) * _borderNoise;
        var warped = new Vector3(position.X + warpX, 0, position.Z + warpZ);

        var best = _seeds[0];
        float bestDist = float.MaxValue;

        foreach (var seed in _seeds)
        {
            float dist = seed.pos.DistanceSquaredTo(warped);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = seed;
            }
        }

        return best.biome;
    }

    public ImageTexture CreateTexture(int resolution)
    {
        var image = Image.CreateEmpty(resolution, resolution, false, Image.Format.Rgb8);

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                var world = new Vector3(
                    (x + 0.5f) / resolution * Size, 0,
                    (y + 0.5f) / resolution * Size);
                image.SetPixel(x, y, GetBiomeAt(world).MaskColor());
            }
        }

        return ImageTexture.CreateFromImage(image);
    }
}