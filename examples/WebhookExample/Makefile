DB_CONTAINER=webhook_postgres

.PHONY: db-up migrate clean run

run:
	dotnet run --project .

db-up:
	docker compose -f docker-compose.yml up -d

db-down:
	docker compose -f docker-compose.yml down

db-logs:
	docker logs -f $(DB_CONTAINER)

migrate:
	dotnet ef database update

clean:
	dotnet clean
	rm -rf bin obj
