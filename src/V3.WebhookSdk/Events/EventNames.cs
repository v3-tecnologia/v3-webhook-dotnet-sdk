namespace V3.WebhookSdk.Events
{
    public static class OrderEventNames
    {
        public const string Ack = "ORDER_STATUS_ACK";
        public const string Sent = "ORDER_STATUS_SENT";
        public const string Failed = "ORDER_STATUS_FAILED";
    }

    public static class VehicleEventNames
    {
        public const string IgnitionOn = "IGNITION_ON";
        public const string IgnitionOff = "IGNITION_OFF";
    }

    public static class DmsEventNames
    {
        public const string Yawning = "YAWNING";
        public const string Drowsiness = "DROWSINESS";
        public const string Drinking = "DRINKING";
        public const string Eating = "EATING";
        public const string EyeClosure = "EYE_CLOSURE";
        public const string GazeDistraction = "GAZE_DISTRACTION";
        public const string GazeFixation = "GAZE_FIXATION";
        public const string OnPhone = "ON_PHONE";
        public const string PoseDistractionPitch = "POSE_DISTRACTION_PITCH";
        public const string PoseDistractionYaw = "POSE_DISTRACTION_YAW";
        public const string Smoking = "SMOKING";
    }

    public static class DriverBehaviorEventNames
    {
        public const string HarshAcceleration = "HARSH_ACCELERATION";
        public const string HarshBraking = "BRAKING_HARSH";
        public const string CorneringHarsh = "CORNERING_HARSH";
        public const string MaxSpeedExceeded = "MAX_SPEED_EXCEEDED";
        public const string NormalSpeedReturn = "RETURN_TO_NORMAL_SPEED";
        public const string PersistentMaxSpeed = "PERSISTENT_MAX_SPEED";
        public const string StartOvertaking = "START_OVERTAKING";

    }
}
