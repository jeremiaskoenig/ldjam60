namespace LudumDare60;

using System.Linq;
using Godot;

public class SelfRightingHelper
{
    private readonly VehicleBody3D vehicle;
    private readonly VehicleWheel3D[] wheels;

    public float RecoveryStrength { get; set; } = 100.0f;
    public float RecoveryDamping { get; set; } = 15.0f;
    public float MaxRecoveryTorque { get; set; } = 250.0f;
    public float TiltThresholdDegrees { get; set; } = 50.0f;
    public float AirborneGracePeriod { get; set; } = 0.35f;

    private float airborneTime;

    public SelfRightingHelper(VehicleBody3D vehicle)
    {
        this.vehicle = vehicle;
        wheels = vehicle.FindChildren("Wheel_*").OfType<VehicleWheel3D>().ToArray();
    }

    public void MoveUpright(double delta)
    {
        int contacts = wheels.Where(wheel => wheel.IsInContact()).Count();
        if (contacts == 0)
            airborneTime += (float)delta;
        else
            airborneTime = 0.0f;

        Vector3 vehicleUp = vehicle.GlobalBasis.Y.Normalized();
        Vector3 worldUp = Vector3.Up;

        float upDot = Mathf.Clamp(vehicleUp.Dot(worldUp), -1.0f, 1.0f);
        float tiltAngle = Mathf.RadToDeg(Mathf.Acos(upDot));
        bool upsideDown = upDot < -0.25f;

        bool tippedOver = contacts <= 2 && tiltAngle > TiltThresholdDegrees;
        bool tiltedInAir = contacts == 0 && airborneTime > AirborneGracePeriod && tiltAngle > TiltThresholdDegrees;

        if (!upsideDown && !tippedOver && !tiltedInAir)
            return;

        Vector3 correctionAxis = vehicleUp.Cross(worldUp);

        if (correctionAxis.LengthSquared() < 0.0001f)
        {
            correctionAxis = vehicle.GlobalBasis.X.Normalized();
        }
        else
        {
            correctionAxis = correctionAxis.Normalized();
        }

        float angleRadians = Mathf.Acos(upDot);

        Vector3 angularVelocity = vehicle.AngularVelocity;
        Vector3 tiltAngularVelocity = angularVelocity - worldUp * angularVelocity.Dot(worldUp);
        Vector3 torque = correctionAxis * (angleRadians * RecoveryStrength) - tiltAngularVelocity * RecoveryDamping;

        float torqueLength = torque.Length();
        if (torqueLength > MaxRecoveryTorque)
        {
            torque = torque / torqueLength * MaxRecoveryTorque;
        }

        vehicle.ApplyTorque(torque);
    }
}