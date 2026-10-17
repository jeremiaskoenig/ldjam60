namespace LudumDare60;

using Godot;
using System;
using System.Linq;


public partial class PlayerController : Node3D
{
    [Export]
    public VehicleBody3D Vehicle { get; set; }

    [Export]
    public CpuParticles3D[] TrailParticles { get; set; } = [];

    [Export]
    public float MaximumSteeringAngle { get; set; } = 0.5f;

    [Export]
    public float MaximumEngineForce { get; set; } = 300;

    [Export]
    public float TopSpeed { get; set; } = 30;

    [Export]
    public float MaximumAcceleration { get; set; } = 10;

    public override void _Ready()
    {
        AddToGroup("Vehicles");
    }

    public override void _Process(double delta)
    {
        
    }

    public override void _PhysicsProcess(double delta)
    {
        UpdateEngineForce();

        if (Input.IsActionPressed("steer_left"))
        {
            Vehicle.Steering = Mathf.MoveToward(Vehicle.Steering, MaximumSteeringAngle, (float)(delta * 10));
        }
        else if (Input.IsActionPressed("steer_right"))
        {
            Vehicle.Steering = Mathf.MoveToward(Vehicle.Steering, -MaximumSteeringAngle, (float)(delta * 10));
        }
        else
        {
            Vehicle.Steering = Mathf.MoveToward(Vehicle.Steering, 0, (float)(delta * 10));
        }

        var velocity = Vehicle.LinearVelocity.Length();

        (FindChild("Label3D") as Label3D).Text = $"{velocity:0}";

        var particleVelocityFactor = velocity / TopSpeed;
        GD.Print($"velocity: {velocity}");
        foreach (var particleEmitter in TrailParticles)
        {
            if (velocity == 0)
            {
                particleEmitter.Emitting = false;
            }
            else
            {
                particleEmitter.Emitting = true;
                particleEmitter.Lifetime = Mathf.Lerp(0.3f, 0.05f, particleVelocityFactor);
                var quad = particleEmitter.Mesh as QuadMesh;
                var size = Mathf.Lerp(0.01f, 0.5f, particleVelocityFactor);
                quad.Size = new Vector2(size, size);
            }

        }
    }

    private bool arcadeSpeedMode = false;

    private void UpdateEngineForce()
    {
        float speed = Vehicle.LinearVelocity.Length();
        if (Input.IsActionPressed("forward"))
        {
            Vehicle.EngineForce = MaximumEngineForce;
        }
        else if (Input.IsActionPressed("backward"))
        {
            Vehicle.EngineForce = -MaximumEngineForce;
        }
        else
        {
            Vehicle.EngineForce = 0;
        }

        if (Input.IsActionPressed("brake"))
        {
            Vehicle.Brake = 10;
        }
        else
        {
            Vehicle.Brake = 0;
            if (speed > 30)
            {
                Vehicle.LinearDamp = 1;
            }
            else
            {
                Vehicle.LinearDamp = 0;
            }
        }
    }

}
