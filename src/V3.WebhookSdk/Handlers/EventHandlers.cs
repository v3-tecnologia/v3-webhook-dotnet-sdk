using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Domain.Events.V1;
using Domain.Orders.V1;
using Domain.Location.V1;

using OrderEventStatus = Domain.Events.V1.OrderStatus;

namespace V3.WebhookSdk.Handlers
{
    public class EventContext
    {
        public string Id { get; set; } = default!;
        public Status Status { get; set; }
        public Timestamp CreatedAt { get; set; } = default!;
        public EventType Type { get; set; }
        public EventCategory Category { get; set; }
        public EventSub Sub { get; set; }

        public Device? Device { get; set; }

        public OrderEventStatus? Order { get; set; }

        public Location? Location { get; set; }
    }

    public delegate Task DmsEventHandler(EventContext context, Dms dmsEvent);
    public delegate Task OrderEventHandler(EventContext context, OrderEventStatus orderEvent);
    public delegate Task ConnectionEventHandler(EventContext context, Connection connectionEvent);
    public delegate Task VisionEventHandler(EventContext context, Vision visionEvent);
    public delegate Task HardwareEventHandler(EventContext context, Health hardwareEvent);

    public delegate Task SystemEventHandler(EventContext context, Domain.Events.V1.System systemEvent);

    public delegate Task TelemetryEventHandler(EventContext context, Telemetry telemetryEvent);
    public delegate Task AlertEventHandler(EventContext context, Alert alertEvent);
    public delegate Task DriverBehaviorEventHandler(EventContext context, DriverBehavior driverBehaviorEvent);
    public delegate Task VehicleEventHandler(EventContext context, Vehicle vehicleEvent);
}
