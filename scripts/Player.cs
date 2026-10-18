namespace LudumDare60;

using Godot;
using LudumDare60;
using System;

public partial class Player : Node3D
{
    [Export]
    public Vehicle Vehicle { get; set; }

    [Export]
    public int PlayerName { get; set; }

    [Export]
    public float CheckpointDistance { get; set; } = 10f;

    public int CurrentLap { get; set; }
    public bool HasFinished { get; set; }
    public float[] RoundTimings { get; set; }
    public float[] RoundDistance { get; set; }

    public Node3D MiniMapMarker { get; set; }

    private Vector3 lastPosition = Vector3.Zero;

    public override void _Ready()
    {
        AddToGroup("Players");
        lastPosition = Vehicle.GlobalPosition;
    }

    public override void _Process(double delta)
    {
        if (!GameState.Instance.HasRaceStarted)
        {
            SetPhysicsProcess(false);
        }
        else
        {
            SetPhysicsProcess(true);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!GameState.IsInitialized)
            return;

        Initialize();
        if (GameState.Instance.HasRaceStarted && !HasFinished && CurrentLap < RoundTimings.Length)
        {
            RoundTimings[CurrentLap] += (float)delta;
            RoundDistance[CurrentLap] += Vehicle.GlobalPosition.DistanceTo(lastPosition);
            lastPosition = Vehicle.GlobalPosition;

            var distance = GameState.Instance.Checkpoint.GlobalPosition.DistanceTo(Vehicle.GlobalPosition);
            if (distance < CheckpointDistance && RoundDistance[CurrentLap] >= GameState.Instance.Track.Curve.GetBakedLength() * 0.8)
            {
                CurrentLap++;
            }
        }
    }

    private void Initialize()
    {
        RoundDistance ??= new float[GameState.Instance.LapCount];
        RoundTimings ??= new float[GameState.Instance.LapCount];
    }
}
