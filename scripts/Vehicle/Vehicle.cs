namespace LudumDare60;

using Godot;
using System;
using System.Linq;

public partial class Vehicle : VehicleBody3D
{
    [Export]
    public CpuParticles3D[] TrailParticles { get; set; } = [];

    [Export]
    public float MaximumSteeringAngle { get; set; } = 0.5f;

    [Export]
    public float MaximumEngineForce { get; set; } = 300;

    [Export]
    public float MaximumBrake { get; set; } = 10;

    [Export]
    public float TopSpeed { get; set; } = 30;

    public float CurrentEngineForce { get; set; }
    public float CurrentSteering { get; set; }
    public float CurrentBrake { get; set; }

    public float CurrentSpeed => LinearVelocity.Length();

    private SelfRightingHelper selfRighting;

    public override void _Ready()
    {
        AddToGroup("Vehicles");
        selfRighting = new SelfRightingHelper(this);
    }

    public override void _PhysicsProcess(double delta)
    {
        EngineForce = CurrentEngineForce;
        Brake = CurrentBrake;
        Steering = Mathf.MoveToward(Steering, CurrentSteering, (float)(delta * 10));

        if (CurrentSpeed > TopSpeed)
        {
            LinearDamp = 1;
        }
        else
        {
            LinearDamp = 0;
        }
        
        var particleVelocityFactor = CurrentSpeed / TopSpeed;
        foreach (var particleEmitter in TrailParticles)
        {
            if (CurrentSpeed == 0)
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

        selfRighting.MoveUpright(delta);
    }

    public void AccelerateForwards(float percent = 1.0f)
    {
        CurrentEngineForce = MaximumEngineForce * percent.Clamped();
    }

    public void AccelerateBackwards(float percent = 1.0f)
    {
        CurrentEngineForce = -MaximumEngineForce * percent.Clamped();
    }

    public void Decelerate()
    {
        CurrentEngineForce = 0;
    }

    public void EngageBrake()
    {
        CurrentBrake = MaximumBrake;
    }

    public void DisengageBrake()
    {
        CurrentBrake = 0;
    }
}

static class Helpers
{
    public static float Clamped(this float value, float min = 0, float max = 1) => Mathf.Clamp(value, min, max);
}