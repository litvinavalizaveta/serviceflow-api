# Local Database

ServiceFlow uses PostgreSQL for local persistence.

## Start PostgreSQL

```bash
docker compose up -d postgres
```

The development connection string is:

```text
Host=localhost;Port=5432;Database=serviceflow;Username=serviceflow;Password=serviceflow
```

This password is only for local Docker development.

If another local PostgreSQL server already owns `localhost:5432`, stop it or override `ConnectionStrings__ServiceFlowDb` with a host/port that reaches the Docker-published database.

## Apply Migrations

```bash
~/.dotnet/dotnet tool restore
~/.dotnet/dotnet dotnet-ef database update \
  --project src/ServiceFlow.Infrastructure \
  --startup-project src/ServiceFlow.Infrastructure
```

## Add a Migration

```bash
~/.dotnet/dotnet dotnet-ef migrations add MigrationName \
  --project src/ServiceFlow.Infrastructure \
  --startup-project src/ServiceFlow.Infrastructure \
  --output-dir Persistence/Migrations
```

## Seed Development Data

Seed data is intentionally opt-in so the API can still start without a local database.

```bash
SeedData__RunOnStartup=true ~/.dotnet/dotnet run --project src/ServiceFlow.Api
```

The seeder runs migrations first, then inserts realistic demo clients, service requests, comments, and audit logs if the database is empty.

## Run Tests

```bash
~/.dotnet/dotnet test ServiceFlow.slnx
```

Persistence integration tests use Testcontainers and require Docker to be running.
