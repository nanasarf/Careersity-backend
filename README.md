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

## Career catalog API

The first feature slice supports career-category browsing, career search/detail, career skill requirements, and ordered primary career pathways. Public responses expose DTOs and include Published content only.

Public routes:

```text
GET /api/career-categories
GET /api/careers?page=1&pageSize=20&search=engineer&categoryId={guid}
GET /api/careers/{slug}
GET /api/careers/{careerId}/pathway
```

Administrative routes are under `/api/admin/career-categories` and `/api/admin/careers`. They support category and career lifecycle operations, career-skill assignments, pathways, levels, and pathway course assignments.

> **Security warning:** Admin routes are temporarily unsecured because authentication and authorization have not been implemented. Do not expose this API to an untrusted network.

A minimal PowerShell creation flow is:

```powershell
$category = Invoke-RestMethod -Method Post -Uri http://localhost:5080/api/admin/career-categories -ContentType application/json -Body '{"name":"Technology","slug":"technology","description":"Technology careers"}'
Invoke-RestMethod -Method Post -Uri "http://localhost:5080/api/admin/career-categories/$($category.id)/publish"

$careerBody = @{
  careerCategoryId = $category.id
  title = "Software Engineer"
  slug = "software-engineer"
  shortDescription = "Builds reliable software systems."
  detailedDescription = $null
  responsibilities = $null
  estimatedDurationWeeks = 24
} | ConvertTo-Json
$career = Invoke-RestMethod -Method Post -Uri http://localhost:5080/api/admin/careers -ContentType application/json -Body $careerBody
Invoke-RestMethod -Method Post -Uri "http://localhost:5080/api/admin/careers/$($career.id)/publish"
Invoke-RestMethod -Uri "http://localhost:5080/api/careers/software-engineer"
```

Career endpoints require PostgreSQL. When the connection string is absent, `/health` and Swagger remain available while career-catalog requests return `503 Service Unavailable` with Problem Details.

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

The current migration covers learning-content structure only: careers, pathways, skills, courses, lessons, assessments, and projects. Case-insensitive answer-option text uniqueness is enforced by the Domain; persistence-level enforcement is deferred. Career catalog APIs are implemented, but course administration, users, authentication, authorization, enrollment, learner progress, assessment attempts, and project submissions are not.

## Current status

This repository contains the Domain and EF Core persistence foundations plus the first career-catalog Application/API vertical slice. The committed connection string is an empty placeholder and contains no secret.
