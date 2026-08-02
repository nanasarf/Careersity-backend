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

Administrative routes are under `/api/admin/career-categories` and `/api/admin/careers`. They support category and career lifecycle operations, career-skill assignments, pathways, levels, and pathway course assignments. Every `/api/admin/**` endpoint requires an Administrator bearer token; anonymous callers receive `401` and Learners receive `403`.

A minimal PowerShell creation flow is:

```powershell
$headers = @{ Authorization = "Bearer $accessToken" }
$category = Invoke-RestMethod -Method Post -Uri http://localhost:5080/api/admin/career-categories -Headers $headers -ContentType application/json -Body '{"name":"Technology","slug":"technology","description":"Technology careers"}'
Invoke-RestMethod -Method Post -Uri "http://localhost:5080/api/admin/career-categories/$($category.id)/publish" -Headers $headers

$careerBody = @{
  careerCategoryId = $category.id
  title = "Software Engineer"
  slug = "software-engineer"
  shortDescription = "Builds reliable software systems."
  detailedDescription = $null
  responsibilities = $null
  estimatedDurationWeeks = 24
} | ConvertTo-Json
$career = Invoke-RestMethod -Method Post -Uri http://localhost:5080/api/admin/careers -Headers $headers -ContentType application/json -Body $careerBody
Invoke-RestMethod -Method Post -Uri "http://localhost:5080/api/admin/careers/$($career.id)/publish" -Headers $headers
Invoke-RestMethod -Uri "http://localhost:5080/api/careers/software-engineer"
```

Career endpoints require PostgreSQL. When the connection string is absent, `/health` and Swagger remain available while career-catalog requests return `503 Service Unavailable` with Problem Details.

## Skill and course content administration

Administrators can manage reusable skills, courses, ordered lessons, prerequisites, and course-skill associations. Public endpoints expose Published content only:

```text
GET /api/skills
GET /api/skills/{slug}
GET /api/courses
GET /api/courses/{slug}
GET /api/courses/{courseSlug}/lessons/{lessonSlug}
```

Protected management routes are rooted at `/api/admin/skills` and `/api/admin/courses` and require an Administrator bearer token. Published and Archived skills and courses are immutable in this version. Courses can be published only when they contain a required lesson, at least one associated and primary Published skill, and only Published prerequisite courses. Prerequisite insertion traverses the existing dependency graph and rejects direct or indirect cycles.

A compact PowerShell administration flow is:

```powershell
$headers = @{ Authorization = "Bearer $accessToken" }
$skill = Invoke-RestMethod -Method Post -Uri http://localhost:5080/api/admin/skills -Headers $headers -ContentType application/json -Body (@{
  name="C#"; slug="csharp"; description="C# programming"; category="Technical"
} | ConvertTo-Json)
Invoke-RestMethod -Method Post -Uri "http://localhost:5080/api/admin/skills/$($skill.id)/publish" -Headers $headers

$course = Invoke-RestMethod -Method Post -Uri http://localhost:5080/api/admin/courses -Headers $headers -ContentType application/json -Body (@{
  title="C# Foundations"; slug="csharp-foundations"; shortDescription="Learn C#."; detailedDescription=$null
  difficulty="Foundation"; estimatedDurationMinutes=120
} | ConvertTo-Json)
$lesson = Invoke-RestMethod -Method Post -Uri "http://localhost:5080/api/admin/courses/$($course.id)/lessons" -Headers $headers -ContentType application/json -Body (@{
  title="Introduction"; slug="introduction"; summary="Start here"; content="Lesson content"
  contentType="Article"; externalResourceUrl=$null; estimatedDurationMinutes=15; order=0; isRequired=$true
} | ConvertTo-Json)
Invoke-RestMethod -Method Post -Uri "http://localhost:5080/api/admin/courses/$($course.id)/skills" -Headers $headers -ContentType application/json -Body (@{
  skillId=$skill.id; proficiencyLevel="Beginner"; isPrimary=$true
} | ConvertTo-Json)
# For a prerequisite, POST prerequisiteCourseId and isRequired to:
# /api/admin/courses/{courseId}/prerequisites
Invoke-RestMethod -Method Post -Uri "http://localhost:5080/api/admin/courses/$($course.id)/publish" -Headers $headers
Invoke-RestMethod -Uri "http://localhost:5080/api/courses/$($course.slug)"
```

## Assessment and project content administration

Administrators can author multiple assessments and practical projects for a course. Assessment authoring supports `SingleChoice`, `MultipleChoice`, and `TrueFalse` questions, ordered answer options, atomic reordering, validation, publication, and archiving. Project authoring supports instructions, expected output, evaluation criteria, submission type, duration, publication, and archiving.

Public course-activity routes expose Published content only under a Published course:

```text
GET /api/courses/{courseSlug}/assessments
GET /api/courses/{courseSlug}/projects
GET /api/courses/{courseSlug}/projects/{projectId}
```

Assessment responses are summaries only. Correct options, answer keys, and Administrator authoring DTOs are never returned publicly. Full project instructions are available through the public project-detail route.

Protected Administrator routes are rooted at `/api/admin/assessments` and `/api/admin/projects`. Assessment subroutes manage questions and answer options, including `/questions/reorder` and `/answer-options/reorder`. All require an Administrator bearer token. Published and Archived assessments and projects are immutable; Published content may be archived but cannot return to Draft.

An assessment may be published only when its parent course is Published, it has at least one question, question and option orders are contiguous from zero, every question is valid for its type, and total points are positive. Projects require a Published parent course and retain Domain validation for required descriptions, instructions, and positive duration.

A compact PowerShell flow is:

```powershell
$headers = @{ Authorization = "Bearer $accessToken" }
$assessment = Invoke-RestMethod -Method Post -Uri http://localhost:5080/api/admin/assessments -Headers $headers -ContentType application/json -Body (@{
  courseId=$courseId; title="SQL Fundamentals"; description="Checks SQL concepts"
  passingScorePercentage=70; maximumAttempts=3
} | ConvertTo-Json)
$question = Invoke-RestMethod -Method Post -Uri "http://localhost:5080/api/admin/assessments/$($assessment.id)/questions" -Headers $headers -ContentType application/json -Body (@{
  prompt="Which statement reads rows?"; questionType="SingleChoice"; order=0; points=1
} | ConvertTo-Json)
Invoke-RestMethod -Method Post -Uri "http://localhost:5080/api/admin/assessments/$($assessment.id)/questions/$($question.id)/answer-options" -Headers $headers -ContentType application/json -Body (@{
  text="SELECT"; isCorrect=$true; order=0
} | ConvertTo-Json)
# Add at least one additional option before publication.
Invoke-RestMethod -Method Post -Uri "http://localhost:5080/api/admin/assessments/$($assessment.id)/publish" -Headers $headers

$project = Invoke-RestMethod -Method Post -Uri http://localhost:5080/api/admin/projects -Headers $headers -ContentType application/json -Body (@{
  courseId=$courseId; title="Portfolio Project"; description="Build a practical solution"
  instructions="Implement and document the project"; expectedOutput="Repository URL"
  evaluationCriteria="Correctness and clarity"; submissionType="RepositoryUrl"; estimatedDurationMinutes=180
} | ConvertTo-Json)
Invoke-RestMethod -Method Post -Uri "http://localhost:5080/api/admin/projects/$($project.id)/publish" -Headers $headers
Invoke-RestMethod -Uri "http://localhost:5080/api/courses/$courseSlug/assessments"
Invoke-RestMethod -Uri "http://localhost:5080/api/courses/$courseSlug/projects/$($project.id)"
```

Assessment attempts, user answers, grading history, project submissions, reviewer feedback, enrollment, and progress tracking are not implemented.

## Identity and authentication

Careersity uses short-lived HMAC-SHA256 JWT access tokens (15 minutes by default) and rotating refresh tokens (14 days by default). Public registration always creates a Learner. Raw refresh tokens are returned once and only SHA-256 hashes are persisted. Reuse of a rotated token revokes every active session for that user. Password changes require the current password, revoke existing sessions, and return a fresh token pair.

Routes:

```text
POST /api/auth/register          anonymous
POST /api/auth/login             anonymous
POST /api/auth/refresh           anonymous
POST /api/auth/logout            authenticated
POST /api/auth/revoke-all        authenticated
POST /api/auth/change-password   authenticated
GET  /api/users/me               authenticated
PUT  /api/users/me               authenticated
```

Configure the signing key through user secrets or `Jwt__SigningKey`; it must contain at least 32 bytes. No signing key is committed:

```powershell
dotnet user-secrets set "Jwt:SigningKey" "replace-with-a-random-development-key-of-at-least-32-bytes" --project src/Careersity.Api
```

Register and log in from PowerShell:

```powershell
$registration = @{ email="learner@example.test"; firstName="Test"; lastName="Learner"; password="replace-with-a-valid-password"; confirmPassword="replace-with-a-valid-password" } | ConvertTo-Json
$auth = Invoke-RestMethod -Method Post -Uri http://localhost:5080/api/auth/register -ContentType application/json -Body $registration
$login = @{ email="learner@example.test"; password="replace-with-a-valid-password" } | ConvertTo-Json
$auth = Invoke-RestMethod -Method Post -Uri http://localhost:5080/api/auth/login -ContentType application/json -Body $login
Invoke-RestMethod -Uri http://localhost:5080/api/users/me -Headers @{ Authorization = "Bearer $($auth.accessToken)" }
```

Swagger exposes a Bearer authorization control; enter the access token value. Authentication endpoints have per-IP in-process rate limits.

Initial Administrator provisioning is explicit, idempotent, and never migration-driven. Supply `InitialAdmin__Enabled=true`, `InitialAdmin__Email`, `InitialAdmin__FirstName`, `InitialAdmin__LastName`, and `InitialAdmin__Password` through environment variables or user secrets. The initializer creates the account only when no user with that normalized email exists and never overwrites its password.

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

The schema covers learning-content structure plus Users and hashed RefreshTokens. Case-insensitive answer-option text uniqueness is enforced by the Domain; persistence-level enforcement is deferred. Refresh-token history cleanup is also deferred; no background job is included. Password reset, email verification, MFA, social login, enrollment, learner progress, course administration, assessment attempts, and project submissions are not implemented.

## Current status

This repository contains the Domain and EF Core persistence foundations, career catalog vertical slice, and JWT identity/authentication slice. Committed connection strings and JWT signing keys are empty placeholders.
