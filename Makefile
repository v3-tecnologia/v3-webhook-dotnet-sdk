DB_CONTAINER=webhook_postgres
EXAMPLE_PATH=examples/WebhookExample

.PHONY: db-up db-down db-logs build run migrate

db-up:
	docker compose -f $(EXAMPLE_PATH)/docker-compose.yml up -d

db-down:
	docker compose -f $(EXAMPLE_PATH)/docker-compose.yml down

db-logs:
	docker logs -f $(DB_CONTAINER)

build:
	cd src/V3.WebhookSdk && dotnet build

run:
	dotnet run --project $(EXAMPLE_PATH)

migrate:
	dotnet ef database update --project $(EXAMPLE_PATH)
