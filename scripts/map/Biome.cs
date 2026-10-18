using Godot;

public enum Biome { Desert, City, Forest }

public static class BiomeExtensions
{
    public static Color MaskColor(this Biome biome) => biome switch
    {
        Biome.Desert => new Color(1, 0, 0),
        Biome.City => new Color(0, 1, 0),
        _ => new Color(0, 0, 1)
    };

    public static Color DebugColor(this Biome biome) => biome switch
    {
        Biome.Desert => Colors.Yellow,
        Biome.City => Colors.Gray,
        _ => Colors.DarkGreen
    };
}