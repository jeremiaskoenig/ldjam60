namespace LudumDare60;

using System;
using Godot;
using Godot.Collections;
using LudumDare60;

public partial class Powerup : Node3D
{
    [Export]
    public string PowerupKey { get; set; }

    [Export]
    public Dictionary<string, Mesh> PowerupMeshes { get; set; }

    [ExportCategory("Fix Values")]
    [ExportGroup("Compass")]
    [Export]
    public Material CompassMaterial { get; set; }

    [Export]
    public float CompassScaleFactor { get; set; } = 3.5f;

    [ExportGroup("Mirror")]
    [Export]
    public float MirrorScaleFactor { get; set; } = 3.5f;

    [Export]
    public Vector3 MirrorOffset { get; set; }

    [ExportGroup("Gas Canister")]
    [Export]
    public Material GasCanisterMaterial { get; set; }

    [Export]
    public float GasCanisterScaleFactor { get; set; } = 3.5f;

    [Export]
    public Vector3 GasCanisterOffset { get; set; }

    [ExportGroup("Gauge")]
    [Export]
    public Vector3 GaugeOffset { get; set; }

    private Area3D area;
    private AnimationPlayer animationPlayer;
    private MeshInstance3D powerupMeshInstance;

    public override void _Ready()
    {
        area = FindChild("Area3D") as Area3D;
        area.BodyEntered += AreaEnteredHandler;
        animationPlayer = FindChild("AnimationPlayer") as AnimationPlayer;
        animationPlayer.Play("idle");
        animationPlayer.AnimationFinished += AnimationFinishedHandler;
        InitMesh();
    }

    private void InitMesh()
    {
        powerupMeshInstance = FindChild("MeshInstance3D") as MeshInstance3D;
        powerupMeshInstance.Mesh = PowerupMeshes[PowerupKey];
        switch (PowerupKey)
        {
            case "compass":
                powerupMeshInstance.MaterialOverride = CompassMaterial;
                powerupMeshInstance.Scale = Vector3.One * CompassScaleFactor;
                break;
            case "mirror":
                powerupMeshInstance.Scale = Vector3.One * MirrorScaleFactor;
                powerupMeshInstance.Translate(MirrorOffset);
                break;
            case "gas_canister":
                powerupMeshInstance.MaterialOverride = GasCanisterMaterial;
                powerupMeshInstance.Scale = Vector3.One * GasCanisterScaleFactor;
                powerupMeshInstance.Translate(GasCanisterOffset);
                break;
            case "gauge":
                powerupMeshInstance.Translate(GaugeOffset);
                break;
        }
    }

    private void AnimationFinishedHandler(StringName animName)
    {
        if (animName == "pickup")
            QueueFree();
    }

    private void AreaEnteredHandler(Node3D body)
    {
        if (body is Vehicle vehicle && vehicle.GetParent() is PlayerController)
        {
            animationPlayer.Play("pickup");
            GameState.Instance.UnlockPowerup(PowerupKey);
        }
    }
}
