using V3.WebhookSdk.Processing;
using V3.WebhookSdk.Events;
using Domain.Events.V1;
using Xunit;

namespace V3.WebhookSdk.Tests.Integration;

public class WebhookEventProcessorIntegrationTests
{
    
    [Fact]
    public async Task Should_process_order_status_ack_event()
    {
        var json = """
        {
            "id": "hook-1",
            "created_at": "2024-07-29T15:51:28.071Z",
            "attributes": [
                {
                "id": "event-1",
                "status": "STATUS_RECEIVED",
                "type": "EVENT_TYPE_ORDER",
                "category": "EVENT_CATEGORY_ORDER",
                "sub": "EVENT_SUB_ORDER_STATUS",
                "created_at": "2025-12-15T18:55:59.748Z",
                "attributes": {
                    "order": {
                    "id": "order-123",
                    "correlation_id": "corr-456",
                    "group": "ORDER_GROUP_COMMAND",
                    "status": "ORDER_STATUS_ACK",
                    "type": "ADD_WIFI",
                    "created_at": "2025-12-15T18:55:59.000Z",
                    "updated_at": "2025-12-15T18:56:10.000Z"
                    }
                }
                }
            ]
        }
        """;

        var handled = false;

        var processor = new WebhookEventProcessorBuilder()
            .OnOrderEvent(
                OrderEventNames.Ack,
                async (ctx, evt) =>
                {
                    handled = true;
                    Console.WriteLine($"[TEST] Handler called!");
                    Console.WriteLine($"[TEST] ctx.Category: {ctx.Category}, ctx.Sub: {ctx.Sub}, evt.Status: {evt.Status}, evt.Type: {evt.Type}");
                    Console.WriteLine($"[TEST] ctx: {System.Text.Json.JsonSerializer.Serialize(ctx)}");
                    Console.WriteLine($"[TEST] evt: {System.Text.Json.JsonSerializer.Serialize(evt)}");

                    Assert.Equal(EventCategory.Order, ctx.Category);
                    Assert.Equal(EventSub.OrderStatus, ctx.Sub);

                    await Task.CompletedTask;
                }
            )
            .Build();

        await processor.ProcessWebhookAsync(json);

        if (!handled)
        {
            Console.WriteLine(json);
        }
        Assert.True(handled);
    }

    [Fact]
    public async Task Should_process_dms_yawning_event()
    {
        var json = """
        {
            "id": "hook-2",
            "created_at": "2024-07-29T15:51:28.071Z",
            "attributes": [
                {
                "id": "event-2",
                "status": "STATUS_RECEIVED",
                "type": "EVENT_TYPE_GENERAL",
                "category": "EVENT_CATEGORY_DMS",
                "sub": "EVENT_SUB_DMS_ADVANCED",
                "created_at": "2025-12-15T19:02:27.853Z",
                "attributes": {
                    "data": {
                    "trip_event": {
                        "event_group_name": "DMS",
                        "dms": {
                        "event_name": "YAWNING"
                        }
                    }
                    }
                }
                }
            ]
        }
        """;

        var handled = false;

        var processor = new WebhookEventProcessorBuilder()
            .OnDmsEvent(
                DmsEventNames.Yawning,
                async (ctx, evt) =>
                {
                    handled = true;

                    Assert.Equal(EventCategory.Dms, ctx.Category);
                    Assert.Equal(EventSub.DmsAdvanced, ctx.Sub);

                    await Task.CompletedTask;
                }
            )
            .Build();

        await processor.ProcessWebhookAsync(json);

        Assert.True(handled);
    }

    [Fact]
    public async Task Should_process_driver_behavior_harsh_braking()
    {
        var json = """
        {
            "id": "hook-3",
            "created_at": "2024-07-29T15:51:28.071Z",
            "attributes": [
                {
                "id": "event-3",
                "status": "STATUS_RECEIVED",
                "type": "EVENT_TYPE_GENERAL",
                "category": "EVENT_CATEGORY_DRIVER_BEHAVIOR",
                "sub": "EVENT_SUB_DRIVER_BEHAVIOR_ADVANCED",
                "created_at": "2025-12-15T19:02:27.853Z",
                "attributes": {
                    "data": {
                    "trip_event": {
                        "event_group_name": "DRIVER_BEHAVIOR",
                        "driver_behavior": {
                        "event_name": "BRAKING_HARSH"
                        }
                    }
                    }
                }
                }
            ]
        }
        """;

        var handled = false;

        var processor = new WebhookEventProcessorBuilder()
            .OnDriverBehaviorEvent(
                DriverBehaviorEventNames.HarshBraking,
                async (ctx, evt) =>
                {
                    handled = true;

                    Assert.Equal(EventCategory.DriverBehavior, ctx.Category);
                    Assert.Equal(EventSub.DriverBehaviorAdvanced, ctx.Sub);

                    await Task.CompletedTask;
                }
            )
            .Build();

        await processor.ProcessWebhookAsync(json);

        Assert.True(handled);
    }
}
