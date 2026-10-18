namespace LudumDare60;

using Godot;
using System;

public partial class BotController : Player
{
    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        if (!GameState.Instance.HasRaceStarted || !GameState.IsInitialized)
            return;

        var lookaheadTarget = GetLookAheadTarget();
        var localTarget = Vehicle.ToLocal(lookaheadTarget);
        float angle = Mathf.Atan2(localTarget.X, localTarget.Z);
        Vehicle.Steering = Mathf.Clamp(angle, -Vehicle.MaximumSteeringAngle, Vehicle.MaximumSteeringAngle);
        Vehicle.AccelerateForwards();
    }

    private Vector3 GetLookAheadTarget()
    {
        var track = GameState.Instance.Track;
        var vehicleLocalPosition = track.ToLocal(Vehicle.GlobalPosition);
        float currentOffset = track.Curve.GetClosestOffset(vehicleLocalPosition);
        float lookAhead = Mathf.Clamp(Vehicle.CurrentSpeed * 0.5f, 5, 20);
        float trackLenght = track.Curve.GetBakedLength();
        float targetOffset = (currentOffset + lookAhead) % trackLenght;
        Vector3 localTarget = track.Curve.SampleBaked(targetOffset);
        return track.ToGlobal(localTarget);
    }
}
