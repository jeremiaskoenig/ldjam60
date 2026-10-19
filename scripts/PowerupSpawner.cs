using Godot;
using Godot.Collections;
using LudumDare60;
using System;

[Tool]
public partial class PowerupSpawner : Path3D
{
    [Export]
    public PackedScene Powerup { get; set; }

    [Export]
    public float[] PowerupLocations { get; set; }

    [Export]
    public string[] PowerupTypes { get; set; }

    [Export]
    public float GasCanFrequency { get; set; } = 40;

    [Export]
    public float TrackLenght
    {
        get => Curve.GetBakedLength();
        set { /* pseudo read only */ }
    }

    [ExportToolButton("Clear")]
    public Callable ClearPowerups => Callable.From(Clear);

    private void Clear()
    {
        foreach (var child in GetChildren())
        {
            child.Free();
        }
    }
    
    [ExportToolButton("Place")]
    public Callable PlacePowerups => Callable.From(Place);

    private void Place()
    {
        if (PowerupLocations.Length != PowerupTypes.Length)
            return;

        for (int i = 0; i < PowerupLocations.Length; i++)
        {
            var position = PowerupLocations[i];
            var type = PowerupTypes[i];
            var powerup = Powerup.Instantiate<Powerup>();

            powerup.PowerupKey = type;
            AddChild(powerup);
            powerup.Position = Curve.SampleBaked(position);
            powerup.Name = $"powerup_{i}_{type}@{position}";
        }
        PlaceGasCans();
    }

    public void PlaceGasCans()
    {
        for (float f = GasCanFrequency; f < TrackLenght; f += GasCanFrequency)
        {
            var name = $"powerup_gas_canister@{f}";
            var powerup = Powerup.Instantiate<Powerup>();

            if (FindChild(name) != null)
                continue;

            powerup.PowerupKey = "gas_canister";
            AddChild(powerup);
            powerup.Position = Curve.SampleBaked(f);
            powerup.Name = name;
            powerup.Translate(Vector3.Up * 0.05f);
        } 
    }

    public override void _Ready()
    {
        Place();
    }

}
