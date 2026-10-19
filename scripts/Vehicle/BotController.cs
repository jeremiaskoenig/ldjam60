namespace LudumDare60;

using Godot;
using System;

public partial class BotController : Player
{
    public enum BotPersonality
    {
        Unset,
        Balanced,
        Aggressive,
        Careful
    }

    [Export]
    public BotPersonality Personality { get; set; } = BotPersonality.Unset;

    [Export]
    public float PersonalLookaheadOffset { get; set; }

    private RayCast3D frontRay;
    private RayCast3D sideRayLeft;
    private RayCast3D sideRayRight;

    public override void _Ready()
    {
        base._Ready();
        if (Personality == BotPersonality.Unset)
            Personality = (BotPersonality)GD.RandRange(0, 2);

        if (PersonalLookaheadOffset == 0)
            PersonalLookaheadOffset = -3 + GD.Randf() * 6;
        
        frontRay = FindChild("FrontRay") as RayCast3D;
        sideRayLeft = FindChild("SideRayLeft") as RayCast3D;
        sideRayRight = FindChild("SideRayRight") as RayCast3D;
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        if (!GameState.Instance.HasRaceStarted || !GameState.IsInitialized)
            return;

        // calculate target point steering
        var lookaheadTarget = GetLookAheadTarget();
        var localTarget = Vehicle.ToLocal(lookaheadTarget);
        float angle = Mathf.Atan2(localTarget.X, localTarget.Z);

        // steering variance (drunk mode?)
        angle += ((2 * GD.Randf()) - 1) * 0.05f;

        bool slowdownRequired = false;
        // calculate avoidance steering
        if (frontRay.IsColliding())
        {
            bool isLeftSideBlocked = sideRayLeft.IsColliding();
            bool isRightSideBlocked = sideRayRight.IsColliding();

            if (!isLeftSideBlocked && isRightSideBlocked)
                angle -= 0.4f;
            else if (isLeftSideBlocked && !isRightSideBlocked)
                angle += 0.4f;
            else if (!isLeftSideBlocked && !isRightSideBlocked)
                angle += 0.4f;
            else
                slowdownRequired = true;
        }

        Vehicle.CurrentSteering = Mathf.Clamp(angle, -Vehicle.MaximumSteeringAngle, Vehicle.MaximumSteeringAngle);
        Vehicle.AccelerateForwards(GetThrottle(angle, slowdownRequired));
    }

    private float GetThrottle(float angle, bool slowdownRequired)
    {
        var factor = Mathf.Abs(angle) / Vehicle.MaximumSteeringAngle;
        if (factor > 2.5)
        {
            return Switch(0.7f, 0.3f);
        }
        else if (factor > 2)
        {
            return Switch(0.8f, 0.5f);
        }
        else if (factor > 1.5)
        {
            return Switch(0.9f, 0.7f);
        }
        else
        {
            return 1;
        }

        float Switch(float balanced, float careful) => Personality switch
        {
            BotPersonality.Balanced => slowdownRequired ? balanced * 0.6f : balanced,
            BotPersonality.Aggressive => 1,
            BotPersonality.Careful => slowdownRequired ? careful * 0.3f : careful,
            _ => balanced,
        };
    }


    private float LookaheadRangeMax => Personality switch
    {
        BotPersonality.Balanced => 20,
        BotPersonality.Aggressive => 10,
        BotPersonality.Careful => 40,
        _ => 20,
    };

    private float LookaheadRangeMin => Personality switch
    {
        BotPersonality.Balanced => 5,
        BotPersonality.Aggressive => 2,
        BotPersonality.Careful => 10,
        _ => 20,
    };

    private Vector3 GetLookAheadTarget()
    {
        var track = GameState.Instance.Track;
        var vehicleLocalPosition = track.ToLocal(Vehicle.GlobalPosition);
        float currentOffset = track.Curve.GetClosestOffset(vehicleLocalPosition);
        float lookAhead = Mathf.Clamp(Vehicle.CurrentSpeed * 0.5f, LookaheadRangeMin, LookaheadRangeMax);
        float trackLenght = track.Curve.GetBakedLength();
        float targetOffset = (currentOffset + lookAhead) % trackLenght;
        float nextOffset = (targetOffset + 1f) % trackLenght;
        Vector3 localTarget = track.Curve.SampleBaked(targetOffset);
        Vector3 nextPoint = track.Curve.SampleBaked(nextOffset);
        Vector3 tangent = (nextPoint - localTarget).Normalized();
        Vector3 lateral = new Vector3(tangent.Z, 0, -tangent.X);
        localTarget += lateral * PersonalLookaheadOffset;
        return track.ToGlobal(localTarget);
    }
}
