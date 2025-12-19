<p align="center">
    <img src="./.github/logo.png" width="200px">
</p>

<h1 align="center" style="font-weight: bold;">v3-webhook-dotnet-sdk</h1>

A .NET SDK for processing and handling **V3 Tecnologia IoT Webhooks**, built on top of **strongly-typed Protocol Buffers models**.

This SDK is **transport-agnostic** (no HTTP server dependency) and can be integrated with **any .NET web framework** (ASP.NET Core, Minimal APIs, Azure Functions, background services, etc.).

---

## Features

- ✅ Strongly-typed event models generated from V3 Protobuf definitions
- ✅ Native JSON → Protobuf parsing (snake_case JSON → camelCase proto)
- ✅ Modular event processor with **group + event name routing**
- ✅ Fluent, declarative API (no giant switch/case)
- ✅ Multiple handlers registered via builder pattern
- ✅ Optional webhook signature validation (HMAC SHA256)
- ✅ No HTTP framework coupling
- ✅ Integration tests covering real payloads

---

## Installation

Clone this repository and reference the SDK project:

```bash
dotnet add reference V3.WebhookSdk/V3.WebhookSdk.csproj
```

---

## Getting Started

### 1. Create the Event Processor

Use `WebhookEventProcessorBuilder` to register handlers for the events you want to process.

Handlers are registered using a **selector-based API**, avoiding procedural dispatch and large switch statements.

```csharp
var processor = new WebhookEventProcessorBuilder()
    .OnEvent(
        EventSelector.Of().Group("ORDER").EventName(OrderEventNames.Ack).Build(),
        async (EventContext ctx, UploadEvent evt) =>
        {
            Console.WriteLine($"Upload event received: {evt.Id}");
            await Task.CompletedTask;
            return EventHandlingResult.Success();
        }
    )
    .OnEvent(
        EventSelector.Of().Group("DMS").EventName(DmsEventNames.Yawning).Build(),
        async (EventContext ctx, YawningEvent evt) =>
        {
            Console.WriteLine($"Order ACK received: {evt.Id}");
            await Task.CompletedTask;
            return EventHandlingResult.Success();
        }
    )
    .Build();
```

>__NOTE__: You can declare **multiple `OnEvent` handlers** in the same builder.  

---

### 2. (Optional) Enable Webhook Signature Validation

If you wish to validate the payload signature:

```csharp
var processor = new WebhookEventProcessorBuilder()
    .WithHmacSha256("your-secret-key")
    .OnEvent(
        EventSelector
            .Of()
            .Group("SYSTEM")
            .EventName("REBOOT"),
        async (ctx, evt) =>
        {
            await Task.CompletedTask;
        }
    )
    .Build();
```

---

### 3. Process Incoming Webhooks

Pass the **raw JSON payload** directly to the processor:

```csharp
await processor.ProcessWebhookAsync(jsonPayload, signature);
```

The SDK will:

- Parse JSON into Protobuf
- Resolve the event group and event name
- Locate the correct handler via the selector
- Invoke the handler with a **strongly-typed Protobuf event**

---

## Event Context

Every handler receives an `EventContext` object with common metadata:

```csharp
public class EventContext
{
    public string Id { get; set; }
    public Status Status { get; set; }
    public Timestamp CreatedAt { get; set; }

    public EventType Type { get; set; }
    public EventCategory Category { get; set; }
    public EventSub Sub { get; set; }

    public Device? Device { get; set; }
    public OrderStatus? Order { get; set; }
    public Location? Location { get; set; }
}
```

---

## Event Payloads

Each handler receives the **concrete Protobuf type** associated with the selector.

---

## Supported Event Domains

The SDK currently supports the following event groups:

- ORDER
- DMS (Driver Monitoring System)
- CONNECTION
- VISION
- HEALTH / HARDWARE
- SYSTEM
- TELEMETRY
- ALERT
- DRIVER_BEHAVIOR
- VEHICLE

---

## A more complete example

Handle DMS Events

1. Implementing persistence
```csharp
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using V3.WebhookSdk.Handlers;
using WebhookExample.Data;
using Google.Protobuf;
using System.Text.Json;

namespace WebhookExample.Persistence
{
    public class PostgresEventReader : IEventReader
    {
        private readonly WebhookDbContext _db;
        public PostgresEventReader(WebhookDbContext db)
        {
            _db = db;
        }

        public async Task<TEvent?> GetEventByIdAsync<TEvent>(string id) where TEvent : class, IMessage
        {
            var entity = await _db.Events.FindAsync(int.Parse(id));
            if (entity == null) return null;
            var evt = (TEvent)Activator.CreateInstance(typeof(TEvent))!;
            evt.MergeFrom(System.Text.Json.JsonSerializer.Deserialize<byte[]>(entity.Payload));
            return evt;
        }

        public async Task<IReadOnlyList<TEvent>> GetEventsAsync<TEvent>(int max = 10) where TEvent : class, IMessage
        {
            var entities = await _db.Events.OrderByDescending(e => e.ReceivedAt).Take(max).ToListAsync();
            var list = new List<TEvent>();
            foreach (var entity in entities)
            {
                var evt = (TEvent)Activator.CreateInstance(typeof(TEvent))!;
                evt.MergeFrom(System.Text.Json.JsonSerializer.Deserialize<byte[]>(entity.Payload));
                list.Add(evt);
            }
            return list;
        }

        public Task<TEvent?> GetRootEventAsync<TEvent>(TEvent childEvent) where TEvent : class, IMessage
        {
            // Implementação customizada se necessário
            return Task.FromResult<TEvent?>(null);
        }
    }

    public class PostgresEventWriter : IEventWriter
    {
        private readonly WebhookDbContext _db;
        public PostgresEventWriter(WebhookDbContext db)
        {
            _db = db;
        }

        public async Task SaveAsync<TEvent>(EventContext context, TEvent evt) where TEvent : Google.Protobuf.IMessage
        {
            var entity = new WebhookEvent
            {
                EventType = context.Type.ToString(),
                EventGroup = context.PayloadKind.ToString(),
                EventName = context.Status.ToString(),
                Payload = JsonSerializer.Serialize(evt),
                ReceivedAt = DateTime.UtcNow
            };
            _db.Events.Add(entity);
            await _db.SaveChangesAsync();
        }
    }
}
```

2. Create some auxiliary classes
```csharp
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using V3.WebhookSdk.Handlers;

namespace WebhookExample.Utils
{
    public static class EventHandlerUtil
    {
        public static async Task<V3.WebhookSdk.Handlers.EventHandlingResult> DispatchAsync<TEvent>(
            ILogger logger,
            EventContext ctx,
            TEvent evt,
            Func<EventContext, TEvent, Task> persistAction)
        {
            var contextJson = JsonSerializer.Serialize(ctx, new JsonSerializerOptions { WriteIndented = true });
            var eventJson = JsonSerializer.Serialize(evt, new JsonSerializerOptions { WriteIndented = true });

            logger.LogInformation($"""
==================== [EVENT RECEIVED] ====================
Type:    {typeof(TEvent).Name}
Kind:    {ctx.PayloadKind}
Context: {contextJson}
Event:   {eventJson}
==========================================================
""");

            await persistAction(ctx, evt);
            return EventHandlingResult.Success();
        }
    }
}
```

3. Create Handlers Factory
```csharp
using Microsoft.Extensions.DependencyInjection;

using Domain.Events.V1;
using V3.WebhookSdk.Events;
using V3.WebhookSdk.Handlers;
using V3.WebhookSdk.Processing;
using V3.WebhookSdk.Security;

using WebhookExample.Data;
using WebhookExample.Persistence;
using WebhookExample.Utils;

namespace WebhookExample.Factories
{
    public static class DmsWebhookProcessorFactory
    {
        private static readonly EventSelector YawningSelector = EventSelector.Of().Group("DMS").EventName(DmsEventNames.Yawning).Build();
        private static readonly EventSelector DrowsinessSelector = EventSelector.Of().Group("DMS").EventName(DmsEventNames.Drowsiness).Build();
        private static readonly EventSelector DrinkingSelector = EventSelector.Of().Group("DMS").EventName(DmsEventNames.Drinking).Build();
        private static readonly EventSelector EatingSelector = EventSelector.Of().Group("DMS").EventName(DmsEventNames.Eating).Build();
        private static readonly EventSelector EyeClosureSelector = EventSelector.Of().Group("DMS").EventName(DmsEventNames.EyeClosure).Build();
        private static readonly EventSelector GazeDistractionSelector = EventSelector.Of().Group("DMS").EventName(DmsEventNames.GazeDistraction).Build();
        private static readonly EventSelector GazeFixationSelector = EventSelector.Of().Group("DMS").EventName(DmsEventNames.GazeFixation).Build();
        private static readonly EventSelector PoseDistractionPitchSelector = EventSelector.Of().Group("DMS").EventName(DmsEventNames.PoseDistractionPitch).Build();
        private static readonly EventSelector PoseDistractionYawSelector = EventSelector.Of().Group("DMS").EventName(DmsEventNames.PoseDistractionYaw).Build();
        private static readonly EventSelector SmokingSelector = EventSelector.Of().Group("DMS").EventName(DmsEventNames.Smoking).Build();
        private static readonly EventSelector OnPhoneSelector = EventSelector.Of().Group("DMS").EventName(DmsEventNames.OnPhone).Build();

        public static WebhookEventProcessor Create(IServiceProvider sp, string webhookSecret)
        {
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<WebhookDbContext>();
            var writer = new PostgresEventWriter(db);
            var reader = new PostgresEventReader(db);
            var loggerFactory = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("DmsWebhookProcessor");

            var builder = new WebhookEventProcessorBuilder()
                .WithSignatureValidator(new HmacSha256SignatureValidator(webhookSecret))
                .WithPersistence(reader, writer)
                .OnEvent<YawningEvent>(YawningSelector, (EventContext ctx, YawningEvent evt) =>
                    EventHandlerUtil.DispatchAsync(logger, ctx, evt, (c, e) => c.SaveAsync(e)))

                .OnEvent<DrowsinessEvent>(DrowsinessSelector, (EventContext ctx, DrowsinessEvent evt) =>
                    EventHandlerUtil.DispatchAsync(logger, ctx, evt, (c, e) => c.SaveAsync(e)))

                .OnEvent<DrinkingEvent>(DrinkingSelector, (EventContext ctx, DrinkingEvent evt) =>
                    EventHandlerUtil.DispatchAsync(logger, ctx, evt, (c, e) => c.SaveAsync(e)))

                .OnEvent<EatingEvent>(EatingSelector, (EventContext ctx, EatingEvent evt) =>
                    EventHandlerUtil.DispatchAsync(logger, ctx, evt, (c, e) => c.SaveAsync(e)))

                .OnEvent<EyeClosureEvent>(EyeClosureSelector, (EventContext ctx, EyeClosureEvent evt) =>
                    EventHandlerUtil.DispatchAsync(logger, ctx, evt, (c, e) => c.SaveAsync(e)))

                .OnEvent<GazeDistractionEvent>(GazeDistractionSelector, (EventContext ctx, GazeDistractionEvent evt) =>
                    EventHandlerUtil.DispatchAsync(logger, ctx, evt, (c, e) => c.SaveAsync(e)))

                .OnEvent<GazeFixationEvent>(GazeFixationSelector, (EventContext ctx, GazeFixationEvent evt) =>
                    EventHandlerUtil.DispatchAsync(logger, ctx, evt, (c, e) => c.SaveAsync(e)))

                .OnEvent<PoseDistractionPitchEvent>(PoseDistractionPitchSelector, (EventContext ctx, PoseDistractionPitchEvent evt) =>
                    EventHandlerUtil.DispatchAsync(logger, ctx, evt, (c, e) => c.SaveAsync(e)))

                .OnEvent<PoseDistractionYawEvent>(PoseDistractionYawSelector, (EventContext ctx, PoseDistractionYawEvent evt) =>
                    EventHandlerUtil.DispatchAsync(logger, ctx, evt, (c, e) => c.SaveAsync(e)))

                .OnEvent<SmokingEvent>(SmokingSelector, (EventContext ctx, SmokingEvent evt) =>
                    EventHandlerUtil.DispatchAsync(logger, ctx, evt, (c, e) => c.SaveAsync(e)))

                .OnEvent<OnPhoneEvent>(OnPhoneSelector, (EventContext ctx, OnPhoneEvent evt) =>
                    EventHandlerUtil.DispatchAsync(logger, ctx, evt, (c, e) => c.SaveAsync(e)));

            return builder.Build();
        }
    }
}
``` 

4. Create entrypoint `Progam.cs`
```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using V3.WebhookSdk.Processing;
using V3.WebhookSdk.Security;
using WebhookExample;
using WebhookExample.Data;
using WebhookExample.Persistence;
using WebhookExample.Factories;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddDbContext<WebhookDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Host=localhost;Port=5432;Database=webhookdb;Username=postgres;Password=postgres"
    )
);

builder.Services.AddScoped<PostgresEventWriter>();
builder.Services.AddScoped<PostgresEventReader>();

builder.Services.AddSingleton<WebhookEventProcessor>(sp =>
{
    var secret =
        builder.Configuration["Webhook:Secret"]
        ?? throw new InvalidOperationException("Webhook:Secret not configured");

    var dbContext = sp.GetRequiredService<WebhookDbContext>();
    var writer = new PostgresEventWriter(dbContext);
    var reader = new PostgresEventReader(dbContext);

    return new WebhookEventProcessorBuilder()
        .WithSignatureValidator(new HmacSha256SignatureValidator(secret))
        .WithPersistence(reader, writer)
        // Adicione handlers conforme necessário
        .Build();
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WebhookDbContext>();
    db.Database.Migrate();
}

app.MapGet("/health", () => Results.Ok("ok"));

app.MapPost("/hooks/callback/dms", async (string body, IServiceProvider sp) =>
{
    var secret = builder.Configuration["Webhook:Secret"] ?? throw new InvalidOperationException("Webhook:Secret not configured");
    var processor = DmsWebhookProcessorFactory.Create(sp, secret);
    var result = await processor.ProcessWebhookAsync(body);
    return result switch
    {
        { IsSuccess: true } => Results.Accepted(),
        { Exception: WebhookSignatureException } => Results.Problem(title: "Unauthorized", detail: result.ErrorMessage, statusCode: StatusCodes.Status401Unauthorized),
        { Exception: InvalidOperationException } => Results.BadRequest(new { error = result.ErrorMessage }),
        _ => Results.Problem(title: "Internal Server Error", detail: result.ErrorMessage, statusCode: StatusCodes.Status500InternalServerError)
    };
});


app.Run();
``` 
---

## Full Example Project

See `examples/WebhookExample` for a full **ASP.NET Minimal API** example that:

- Receives webhooks
- Validates signatures
- Processes events using this SDK
- Persists data to PostgreSQL

You can run it with:

```bash
make run
```

---

## Design Notes

- Declarative, non-procedural event routing
- No framework lock-in
- Reflection-based dispatch using Protobuf descriptors
- Safe extensibility for new event types without SDK changes