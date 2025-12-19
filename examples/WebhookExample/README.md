# WebhookExample

This project demonstrates how to use the V3 Webhook .NET SDK to receive, process, and persist V3 Tecnologia webhook events in a modular, idiomatic way.

## How It Works

- **Modular Endpoints:** Each event domain (DMS, ORDER, ALERT, etc.) has its own HTTP endpoint and event processor factory, ensuring clean separation of concerns.
- **Event Routing:** Incoming webhook requests are routed to the correct handler based on event context and name, using strongly-typed event models generated from Protobuf.
- **Persistence:** Events are persisted to PostgreSQL using EF Core via the `PostgresEventWriter` and can be read back with `PostgresEventReader`.
- **Logging:** All event handling uses ASP.NET Core's `ILogger` for structured, contextual logging.
- **Dispatcher Utility:** Centralized logic for logging and persistence is handled by the `EventHandlerUtil.DispatchAsync` utility, keeping handlers DRY and maintainable.
- **Integration Tests:** Example payloads and integration tests are provided for each domain to ensure reliability and coverage.

## How to Run

### Prerequisites
- .NET 8 SDK
- Docker (for PostgreSQL)

### 1. Start PostgreSQL with Docker Compose
```bash
make infra
```

### 2. Apply Database Migrations
```bash
make migrate
```

### 3. Run the Example Project
```bash
make run
```
The API will be available at `http://localhost:5000`.

### 4. Send Webhook Events
You can POST example payloads (see `examples/json_payloads/`) to the endpoints:
- `/webhook/dms`
- `/webhook/order`
- `/webhook/alert`

Example:
```bash
curl -X POST http://localhost:5000/webhook/dms \
  -H "Content-Type: application/json" \
  -d @examples/json_payloads/events/driver_behavior/trip_driver_behavior_event.json
```

## Project Structure
- `Factories/` — Per-domain event processor factories
- `Persistence/` — Postgres event writer/reader
- `Utils/` — Dispatcher utility for logging/persistence
- `Program.cs` — DI setup and endpoint mapping
- `examples/json_payloads/` — Example webhook payloads

## Customization
- Add new event handlers by updating the relevant factory and Protobuf models.
- Extend persistence by implementing `IEventWriter`/`IEventReader`.
- Use integration tests as a template for your own event flows.

## Troubleshooting
- Ensure Docker is running and PostgreSQL is healthy before starting the API.
- Check logs for detailed error messages (uses `ILogger`).

---
For more details, see the main SDK documentation.
