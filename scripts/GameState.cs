namespace LudumDare60;

using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public partial class GameState : Node
{
    [ExportGroup("Settings")]
    [Export]
    public int LapCount { get; set; } = 3;

    [ExportGroup("State")]
    [Export]
    public float RaceTime { get; set; }
    [Export]
    public bool HasRaceStarted { get; set; }
    
    [Export]
    public Path3D Track { get; set; }

    [Export]
    public Node3D Checkpoint { get; set; }

    public double Countdown => countdown;
    private double countdown = 5;

    public IEnumerable<Player> Players => GetTree().GetNodesInGroup("Players").OfType<Player>();

    public bool IsRaceFinished => Players.All(p => p.HasFinished);

    public static GameState Instance { get; private set; }

    public static bool IsInitialized => Instance.Track != null && Instance.Checkpoint != null;

    private PlayerController playerController;

    public override void _Ready()
    {
        if (Instance != null && Instance != this)
        {
            GD.PrintErr($"GameState already initialized");
        }
        else
        {
            Instance = this;
        }
    }

    public override void _Process(double delta)
    {
        Initialize();

        if (countdown > -5)
            countdown -= delta;

        if (countdown <= 0)
            GameState.Instance.HasRaceStarted = true;
    }

    public void UnlockPowerup(string key)
    {
        playerController?.EnableUpgrade(key);
    }

    private void Initialize()
    {
        Track ??= GetTree().Root.FindChild("GameTrack", owned: false) as Path3D;
        Checkpoint ??= GetTree().Root.FindChild("LapCheckpoint", owned: false) as Node3D;
        playerController ??= GetTree().GetNodesInGroup("Players").OfType<PlayerController>().FirstOrDefault();
    }
}
