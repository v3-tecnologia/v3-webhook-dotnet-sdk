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
    }

    public static class DriverBehaviorEventNames
    {
        public const string HarshBraking = "HARSH_BRAKING";
    }
}
