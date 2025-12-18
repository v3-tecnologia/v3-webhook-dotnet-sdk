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
- ✅ Explicit handler registration per event domain
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

Use WebhookEventProcessorBuilder to register handlers for the events you want to process.

Each handler is registered using:

* Event Group (SYSTEM, ORDER, TELEMETRY, etc.)

* Event Name (REBOOT, DEVICE_STATE, ORDER_STATUS_ACK, …)

```csharp
var processor = new WebhookEventProcessorBuilder()
    .OnOrderEvent("ORDER_STATUS_ACK", async (ctx, evt) =>
    {
        Console.WriteLine($"Order ACK received: {evt.Id}");
        await Task.CompletedTask;
    })
    .OnSystemEvent("REBOOT", async (ctx, evt) =>
    {
        Console.WriteLine("System reboot event received");
        await Task.CompletedTask;
    })
    .OnTelemetryEvent("IGNITION_STATUS", async (ctx, evt) =>
    {
        Console.WriteLine($"Ignition status: {evt.Status}");
        await Task.CompletedTask;
    })
    .Build();
```

---

### 2. (Optional) Enable Webhook Signature Validation

If you wish to validate the payload signature:

```csharp
var processor = new WebhookEventProcessorBuilder()
    .WithHmacSha256("your-secret-key")
    .OnSystemEvent("REBOOT", async (ctx, evt) =>
    {
        await Task.CompletedTask;
    })
    .Build();
```
---

### 3. Process Incoming Webhooks

Pass the **raw JSON payload** directly to the processor:

```csharp
await processor.ProcessWebhookAsync(jsonPayload, signature);
``` 

* The SDK will:

  * Parse JSON into Protobuf
  * Resolve the event group and event name
  * Select the correct handler
  * Invoke the handler with a typed payload

---

### Event Context

Every handler receives an EventContext object with common metadata:

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

### Supported Event Domains

The SDK currently supports the following event groups:

* ORDER
* DMS (Driver Monitoring System)
* CONNECTION
* VISION
* HEALTH / HARDWARE
* SYSTEM
* TELEMETRY
* ALERT
* DRIVER_BEHAVIOR
* VEHICLE

Each domain maps directly to a Protobuf message and handler delegate.

---

### Example Project

See `examples/WebhookExample` for a full **ASP.NET Minimal API** example that:

* Receives webhooks
* Validates signatures
* Processes events using this SDK
* Persists data to PostgreSQL

You can run it with:

```bash
make db-up
make migrate
make run
```