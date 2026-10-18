namespace LudumDare60;

using Godot;
using System;

public partial class Tunnel : Node3D
{
    [Export(hintString: "Only enable when we fixed that lighting bug")]
    public bool IsTunnelBehaviourEnabled { get; set; }

    private DirectionalLight3D sunlight;

    private float lightTimer;
    private bool dimLight;

    public override void _Ready()
    {
        if (!IsTunnelBehaviourEnabled)
            return;

        var outerArea = FindChild("OuterTunnelArea") as Area3D;
        outerArea.BodyEntered += BodyEnteredLight;
        var innerArea = FindChild("InnerTunnelArea") as Area3D;
        innerArea.BodyExited += BodyExitedLight;

        sunlight = GetTree().Root.FindChild("DirectionalLight3D", owned: false) as DirectionalLight3D;
    }

    private void BodyExitedLight(Node3D body)
    {
        if (body is Vehicle vehicle && vehicle.GetParent() is PlayerController)
        {
            dimLight = false;
            lightTimer = 2;
        }
    }


    private void BodyEnteredLight(Node3D body)
    {
        if (body is Vehicle vehicle && vehicle.GetParent() is PlayerController)
        {
            dimLight = true;
            lightTimer = 2;
        }
    }

    public override void _Process(double delta)
    {
        if (!IsTunnelBehaviourEnabled)
            return;

        if (lightTimer > 0)
        {
            lightTimer -= (float)delta;
            var min = dimLight ? 0f : 1f;
            var max = dimLight ? 1f : 0f;
            var factor = lightTimer * 0.5f;

            sunlight.LightEnergy = Mathf.Clamp(Mathf.Lerp(min, max, factor), 0, 1);
        }
    }

}
