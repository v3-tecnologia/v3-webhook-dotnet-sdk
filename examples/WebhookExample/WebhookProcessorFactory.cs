using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using V3.WebhookSdk.Handlers;
using V3.WebhookSdk.Processing;
using V3.WebhookSdk.Security;
using WebhookExample.Data;

namespace WebhookExample
{
    public static class WebhookProcessorFactory
    {
        public static WebhookEventProcessor Create(
            IServiceProvider sp,
            string webhookSecret
        )
        {
            Func<string, string, Func<EventContext, object, Task>> PersistAndLog =
                (group, name) => async (ctx, evt) =>
                {
                    using var scope = sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<WebhookDbContext>();

                    var entity = new WebhookEvent
                    {
                        EventType = ctx.Type.ToString(),
                        EventGroup = group,
                        EventName = name,
                        Payload = JsonSerializer.Serialize(evt),
                        ReceivedAt = DateTime.UtcNow
                    };

                    db.Events.Add(entity);
                    await db.SaveChangesAsync();

                    Console.WriteLine($"[{group}] {name}");
                };

            static OrderEventHandler Order(Func<EventContext, object, Task> h) =>
                new(async (ctx, evt) => await h(ctx, evt));

            static DmsEventHandler Dms(Func<EventContext, object, Task> h) =>
                new(async (ctx, evt) => await h(ctx, evt));

            static ConnectionEventHandler Connection(Func<EventContext, object, Task> h) =>
                new(async (ctx, evt) => await h(ctx, evt));

            static VisionEventHandler Vision(Func<EventContext, object, Task> h) =>
                new(async (ctx, evt) => await h(ctx, evt));

            static HardwareEventHandler Hardware(Func<EventContext, object, Task> h) =>
                new(async (ctx, evt) => await h(ctx, evt));

            static SystemEventHandler System(Func<EventContext, object, Task> h) =>
                new(async (ctx, evt) => await h(ctx, evt));

            static TelemetryEventHandler Telemetry(Func<EventContext, object, Task> h) =>
                new(async (ctx, evt) => await h(ctx, evt));

            static AlertEventHandler Alert(Func<EventContext, object, Task> h) =>
                new(async (ctx, evt) => await h(ctx, evt));

            static DriverBehaviorEventHandler DriverBehavior(Func<EventContext, object, Task> h) =>
                new(async (ctx, evt) => await h(ctx, evt));

            static VehicleEventHandler Vehicle(Func<EventContext, object, Task> h) =>
                new(async (ctx, evt) => await h(ctx, evt));

            return new WebhookEventProcessorBuilder()
                .WithSignatureValidator(
                    new HmacSha256SignatureValidator(webhookSecret)
                )

                // Order events
                .OnOrderEvent("ORDER_STATUS_ACK",
                    Order(PersistAndLog("ORDER", "ORDER_STATUS_ACK")))
                .OnOrderEvent("ORDER_STATUS_SENT",
                    Order(PersistAndLog("ORDER", "ORDER_STATUS_SENT")))
                .OnOrderEvent("ORDER_STATUS_FAILED",
                    Order(PersistAndLog("ORDER", "ORDER_STATUS_FAILED")))

                // DMS events
                .OnDmsEvent("DROWSINESS",
                    Dms(PersistAndLog("DMS", "DROWSINESS")))
                .OnDmsEvent("GAZE_DISTRACTION",
                    Dms(PersistAndLog("DMS", "GAZE_DISTRACTION")))
                .OnDmsEvent("EYE_CLOSURE",
                    Dms(PersistAndLog("DMS", "EYE_CLOSURE")))

                // Connection events
                .OnConnectionEvent("WIFI_CONNECTED",
                    Connection(PersistAndLog("CONNECTION", "WIFI_CONNECTED")))
                .OnConnectionEvent("WIFI_DISCONNECTED",
                    Connection(PersistAndLog("CONNECTION", "WIFI_DISCONNECTED")))

                // Vision events
                .OnVisionEvent("FACE_DETECTED",
                    Vision(PersistAndLog("VISION", "FACE_DETECTED")))
                .OnVisionEvent("FACE_LOST",
                    Vision(PersistAndLog("VISION", "FACE_LOST")))

                // Hardware / Health events
                .OnHardwareEvent("RESTART",
                    Hardware(PersistAndLog("HEALTH", "RESTART")))
                .OnHardwareEvent("SD_CARD_MOUNTED",
                    Hardware(PersistAndLog("HEALTH", "SD_CARD_MOUNTED")))

                // System events
                .OnSystemEvent("UPLOAD",
                    System(PersistAndLog("SYSTEM", "UPLOAD")))

                // Telemetry events
                .OnTelemetryEvent("IGNITION",
                    Telemetry(PersistAndLog("TELEMETRY", "IGNITION")))
                .OnTelemetryEvent("BATTERY",
                    Telemetry(PersistAndLog("TELEMETRY", "BATTERY")))

                // Alert events
                .OnAlertEvent("CRITICAL",
                    Alert(PersistAndLog("ALERT", "CRITICAL")))
                .OnAlertEvent("WARNING",
                    Alert(PersistAndLog("ALERT", "WARNING")))

                // DriverBehavior events
                .OnDriverBehaviorEvent("HARSH_ACCELERATION",
                    DriverBehavior(PersistAndLog("DRIVER_BEHAVIOR", "HARSH_ACCELERATION")))
                .OnDriverBehaviorEvent("HARSH_BRAKING",
                    DriverBehavior(PersistAndLog("DRIVER_BEHAVIOR", "HARSH_BRAKING")))

                // Vehicle events
                .OnVehicleEvent("IGNITION_OFF",
                    Vehicle(PersistAndLog("VEHICLE", "IGNITION_OFF")))
                .OnVehicleEvent("VEHICLE_EVENT",
                    Vehicle(PersistAndLog("VEHICLE", "VEHICLE_EVENT")))

                .Build();
        }
    }
}
