# V3 Webhook .NET SDK

A .NET library for processing and handling V3 Tecnologia IoT webhooks, based on strongly-typed Protocol Buffers models. This SDK is agnostic (no HTTP server dependency) and can be integrated with any .NET web framework.

## Features

- Strongly-typed event models generated from V3 protobufs
- Automatic JSON parsing and validation
- Modular event processor with handler registration per event type
- Easy integration with any HTTP server (ASP.NET, Minimal API, etc.)
- Example project included with PostgreSQL persistence

## Getting Started

### 1. Install the SDK

Clone this repository and add a reference to `V3.WebhookSdk` in your project:

```
dotnet add reference V3.WebhookSdk/V3.WebhookSdk.csproj
```

### 2. Register Event Handlers

Use the builder to register handlers for each event type you want to process:

```csharp
var processor = new WebhookEventProcessorBuilder()
    .WithOrderHandler("ORDER_STATUS_ACK", async (ctx, evt) => { /* handle order ack */ })
    .WithDmsHandler("DROWSINESS", async (ctx, evt) => { /* handle drowsiness */ })
    // ...add more handlers as needed
    .Build();
```

### 3. Process Incoming Webhooks

Pass the received JSON payload to the processor:

```csharp
await processor.ProcessWebhookAsync(jsonPayload);
```

The processor will parse the payload, map the event, and invoke the appropriate handler automatically.

### 4. Example Project

See `examples/WebhookExample` for a complete ASP.NET Minimal API project that receives webhooks and persists all events to a PostgreSQL database. You can run the example with Docker Compose and the provided Makefile:

```
make db-up
make migrate
make run
```

## Supported Event Types

- Order events
- DMS (Driver Monitoring System) events
- Connection events
- Vision events
- Hardware/Health events
- System events
- Telemetry events
- Alert events
- Driver Behavior events
- Vehicle events

## License

MIT
