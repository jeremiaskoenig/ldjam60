namespace LudumDare60;

using Godot;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

public partial class PlayerController : Player
{
    [Export]
    public Node3D BoosterVisuals { get; set; }

    private Path3D miniMap;
    private Label3D speedLabel;
    private Label3D timeLabel;
    private Label3D countdownLabel;
    private Node3D compass;
    private MeshInstance3D mirror;
    private bool mirrorEnabled;

    private float boostTimer;

    public override void _Ready()
    {
        base._Ready();
        miniMap = FindChild("MiniMap") as Path3D;
        speedLabel = FindChild("SpeedLabel3D") as Label3D;
        timeLabel = FindChild("TimeLabel3D") as Label3D;
        countdownLabel= FindChild("CountdownLabel3D") as Label3D;
        compass = FindChild("Compass") as Node3D;
        mirror = FindChild("Mirror") as MeshInstance3D;

        miniMap.Visible = false;
        speedLabel.Visible = false;
        timeLabel.Visible = false;
        compass.Visible = false;
        mirror.Visible = false;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (!GameState.IsInitialized)
            return;

        UpdateMiniMap();
        UpdateUI();
    }

    public void EnableUpgrade(string key)
    {
        switch (key)
        {
            case "clock":
                timeLabel.Visible = true;
                break;
            case "radio":
                miniMap.Visible = true;
                break;
            case "compass":
                compass.Visible = true;
                break;
            case "gauge":
                speedLabel.Visible = true;
                break;
            case "mirror":
                mirrorEnabled = true;
                break;
            case "gas_canister":
                boostTimer = 5;
                break;
        }
    }

    private static Mesh minimapMarkerMesh = null;

    private static Material BuildMarkerMaterial(string playerName)
    {
        var playerHash = playerName.GetHashCode();
        var color = new Color(((playerHash >> 16) & 0xFF) / 255f, ((playerHash >> 8) & 0xFF) / 255f, (playerHash & 0xFF)  / 255f, 0.5f);
        
        GD.Print($"Creating Material for player {playerName}: {color}");
        return new StandardMaterial3D()
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoColor = color
        };
    }

    private static void InitMinimapMarkerMesh()
    {
        minimapMarkerMesh = new SphereMesh() { Radius = 40f };
    }

    private void UpdateMiniMap()
    {
        if (miniMap.Curve == null)
        {
            var curve = GameState.Instance.Track.Curve;
            miniMap.Curve = curve;
            for (int p = 0; p < curve.PointCount; p++)
            {
                var pos = curve.GetPointPosition(p);
                curve.SetPointPosition(p, new Vector3(pos.X, 0, pos.Y));
            }
        }

        if (minimapMarkerMesh == null)
        {
            InitMinimapMarkerMesh();
        }

        int playerIndex = 0;
        foreach (var player in GameState.Instance.Players)
        {
            if (player.MiniMapMarker == null)
            {
                var playerMarker = new MeshInstance3D()
                {
                    Mesh = minimapMarkerMesh,
                    MaterialOverride = BuildMarkerMaterial($"player_{playerIndex}")
                };
                miniMap.AddChild(playerMarker);
                playerMarker.Translate(Vector3.Down * (50 + 10 * playerIndex));
                player.MiniMapMarker = playerMarker;
                playerIndex++;
            }

            var trackLocalPosition = GameState.Instance.Track.ToLocal(player.Vehicle.GlobalPosition);
            player.MiniMapMarker.Position = trackLocalPosition;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        if (!GameState.IsInitialized)
            return;

        var boostActive = false;
        if (boostTimer > 0)
        {
            boostActive = true;
            boostTimer -= (float)delta;
            Vehicle.CurrentEngineForce = Vehicle.MaximumEngineForce * 1.5f;
        }
        else if (Input.IsActionPressed("forward"))
            Vehicle.AccelerateForwards();
        else if (Input.IsActionPressed("backward"))
            Vehicle.AccelerateBackwards();
        else
            Vehicle.Decelerate();

        if (Input.IsActionPressed("brake"))
            Vehicle.EngageBrake();
        else
            Vehicle.DisengageBrake();

        if (Input.IsActionPressed("steer_left"))
            Vehicle.CurrentSteering = Vehicle.MaximumSteeringAngle;
        else if (Input.IsActionPressed("steer_right"))
            Vehicle.CurrentSteering = -Vehicle.MaximumSteeringAngle;
        else
            Vehicle.CurrentSteering = 0;

        mirror.Visible = mirrorEnabled && Input.IsKeyPressed(Key.Tab);
        BoosterVisuals.Visible = boostActive;
        Vehicle.TopSpeed = boostActive ? 40 : 30;

        if (Input.IsActionJustPressed("debug"))
        {
            GD.Print($"{Vehicle.GlobalPosition.X} | {Vehicle.GlobalPosition.Y} | {Vehicle.GlobalPosition.Z}");
        }
    }

    private void UpdateUI()
    {
        UpdateCountdownUI();
        UpdateTimingUI();
        UpdateSpeedUI();
        UpdateCompassUI();
        UpdateRearViewMirrorUI();
    }

    private void UpdateRearViewMirrorUI()
    {
        var material = mirror.Mesh.SurfaceGetMaterial(0) as StandardMaterial3D;
        if (material.AlbedoTexture == null)
        {
            var viewport = mirror.FindChild("SubViewport") as SubViewport;
            material.AlbedoTexture = viewport.GetTexture();
            material.ResourceLocalToScene = true;
            mirror.Mesh.ResourceLocalToScene = true;
        }
    }

    private void UpdateCompassUI()
    {
        var nextCheckpoint = GameState.Instance.Track.ToGlobal(GetNextCheckpoint());
        nextCheckpoint.Y = compass.GlobalPosition.Y;
        compass.LookAt(nextCheckpoint, Vector3.Forward);
        compass.Rotation = new Vector3(0, compass.Rotation.Y, Mathf.DegToRad(-90));
    }

    private Vector3 GetNextCheckpoint()
    {
        var track = GameState.Instance.Track;
        var curve = track.Curve;

        var playerLocal = track.ToLocal(Vehicle.GlobalPosition);
        var offset = curve.GetClosestOffset(playerLocal);

        for (int i = 0; i < curve.PointCount; i++)
        {
            var pointPosition = curve.GetPointPosition(i);
            var pointOffset = curve.GetClosestOffset(pointPosition);

            if (pointOffset > offset)
            {
                return pointPosition;
            }
        }
        return curve.GetPointPosition(0);
    }

    private void UpdateSpeedUI()
    {
        speedLabel.Text = Vehicle.CurrentSpeed.ToString("0");
    }

    private void UpdateTimingUI()
    {
        if (RoundTimings == null)
            return;

        StringBuilder builder = new();
        for (int i = 0; i < RoundTimings.Length; i++)
        {
            float value = RoundTimings[i];
            builder.AppendLine($"Lap {i + 1}: {value:0.00}");
        }
        timeLabel.Text = builder.ToString();
    }

    private void UpdateCountdownUI()
    {
        var countdown = GameState.Instance.Countdown;
        if (countdown > 0)
        {
            var second = (int)countdown;
            var fraction = second != 0 ? countdown % second : countdown;
            var z = (float)Mathf.Lerp(-0.45, -0.05, fraction);
            countdownLabel.Position = new Vector3(0, 0, z);
            countdownLabel.Text = (second + 1).ToString();
        }
        else if (countdown > -1)
        {
            var fraction = -countdown;
            var z = (float)Mathf.Lerp(-0.45, -0.05, fraction);
            countdownLabel.Position = new Vector3(0, 0, z);
            countdownLabel.Text = "GO!";
            countdownLabel.Modulate = countdownLabel.Modulate.WithAlpha((float)(1 + countdown));
            countdownLabel.OutlineModulate = countdownLabel.OutlineModulate.WithAlpha((float)(1 + countdown));
        }
        else
        {
            countdownLabel.Visible = false;
        }
    }
}
