# Careersity

Careersity is a career-based learning platform that organizes education around job titles and structured learning pathways instead of traditional college majors.

## Architecture

The backend uses Clean Architecture with dependencies pointing inward:

```text
API -> Infrastructure -> Application -> Domain
 \---------------------> Application
```

- `Careersity.Domain`: domain entities, value objects, enums, exceptions, and core business rules.
- `Careersity.Application`: use cases, DTOs, validators, services, and abstractions. References Domain only.
- `Careersity.Infrastructure`: persistence and external-service implementations. References Application and Domain.
- `Careersity.Api`: ASP.NET Core host, HTTP pipeline, OpenAPI, health checks, and composition root.
- `Careersity.UnitTests`: fast tests for Domain and Application.
- `Careersity.IntegrationTests`: API startup and integration tests.

## Prerequisites

- .NET 8 SDK or a newer SDK capable of targeting .NET 8
- PostgreSQL 16 for local persistence work
- Docker Desktop when running the PostgreSQL integration tests

## Build and run

```bash
dotnet restore
dotnet build
dotnet test
dotnet run --project src/Careersity.Api
```

The launch profiles expose:

- HTTP API: `http://localhost:5080`
- HTTPS API: `https://localhost:7080`
- Swagger (Development): `http://localhost:5080/swagger` or `https://localhost:7080/swagger`
- Health check: `http://localhost:5080/health` or `https://localhost:7080/health`

## PostgreSQL persistence

The schema uses plural PascalCase table names and stores GUIDs as PostgreSQL `uuid`, enums as readable strings, and audit timestamps as `timestamp with time zone`. Database registration is currently optional: when `ConnectionStrings:CareersityDatabase` is empty, the API still starts and `/health` remains independent of PostgreSQL.

For local development, set the connection string through an environment variable:

```powershell
$env:ConnectionStrings__CareersityDatabase="Host=localhost;Port=5432;Database=careersity;Username=postgres;Password=postgres"
```

Alternatively, use .NET user secrets from the API project:

```powershell
dotnet user-secrets init --project src/Careersity.Api
dotnet user-secrets set "ConnectionStrings:CareersityDatabase" "Host=localhost;Port=5432;Database=careersity;Username=postgres;Password=postgres" --project src/Careersity.Api
```

Create a local PostgreSQL 16 container when needed:

```powershell
docker run --name careersity-postgres -e POSTGRES_DB=careersity -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=postgres -p 5432:5432 -d postgres:16
```

Create and apply migrations from the repository root:

```powershell
dotnet ef migrations add MigrationName --project src/Careersity.Infrastructure --startup-project src/Careersity.Api --context CareersityDbContext --output-dir Persistence/Migrations
dotnet ef database update --project src/Careersity.Infrastructure --startup-project src/Careersity.Api --context CareersityDbContext
```

Database integration tests use Testcontainers to start an isolated PostgreSQL 16 instance and require a running Docker daemon:

```powershell
docker version
dotnet test tests/Careersity.IntegrationTests/Careersity.IntegrationTests.csproj
```

The current migration covers learning-content structure only: careers, pathways, skills, courses, lessons, assessments, and projects. Case-insensitive answer-option text uniqueness is enforced by the Domain; persistence-level enforcement is deferred. Repositories, feature APIs, users, authentication, enrollment, and learner progress are not implemented.

## Current status

This repository contains the domain and EF Core persistence foundations. Dependency injection, Swagger/OpenAPI, health checks, PostgreSQL mappings and migrations, centralized build settings, and test projects are configured. The committed connection string is an empty placeholder and contains no secret.
