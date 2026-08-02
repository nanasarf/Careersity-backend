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

## Learner career enrollment and progress

Authenticated users can enroll in the current primary Published pathway for a Published career and track their own lesson-based progress. Existing enrollments remain pinned to the pathway version selected at enrollment time.

```text
POST /api/me/career-enrollments
GET  /api/me/career-enrollments
GET  /api/me/career-enrollments/{enrollmentId}
POST /api/me/career-enrollments/{enrollmentId}/pause
POST /api/me/career-enrollments/{enrollmentId}/resume
POST /api/me/career-enrollments/{enrollmentId}/withdraw
GET|POST /api/me/career-enrollments/{enrollmentId}/courses/{courseId}[/start|/complete]
GET|POST /api/me/career-enrollments/{enrollmentId}/courses/{courseId}/lessons/{lessonId}[/start|/complete]
```

All routes infer the user from the JWT and return `404` for another user's enrollment. Active enrollment is required for progress. Paused enrollments may be resumed; Active or Paused enrollments may be withdrawn. Withdrawal preserves history and permits a fresh enrollment. Completed and Withdrawn enrollments cannot receive further activity.

First-level courses are available when their explicit prerequisites are complete. Later levels unlock after every required course in earlier levels is complete; optional earlier courses and same-level ordering do not block progress. Archived content remains visible in history but cannot receive new activity.

Course progress is `completed required lessons / required lessons × 100`. Pathway progress is `completed required pathway courses / required pathway courses × 100`, rounded to two decimals. Optional lessons and courses do not reduce progress. Completing the final required lesson automatically completes its course; completing every required pathway course automatically completes the enrollment. Assessments and projects do not yet affect completion.

PowerShell example:

```powershell
$headers = @{ Authorization = "Bearer $accessToken" }
$enrollment = Invoke-RestMethod -Method Post -Uri http://localhost:5080/api/me/career-enrollments -Headers $headers -ContentType application/json -Body (@{ careerId=$careerId } | ConvertTo-Json)
Invoke-RestMethod -Method Post -Uri "http://localhost:5080/api/me/career-enrollments/$($enrollment.id)/courses/$courseId/start" -Headers $headers
Invoke-RestMethod -Method Post -Uri "http://localhost:5080/api/me/career-enrollments/$($enrollment.id)/courses/$courseId/lessons/$lessonId/complete" -Headers $headers
$progress = Invoke-RestMethod -Uri "http://localhost:5080/api/me/career-enrollments/$($enrollment.id)" -Headers $headers
$progress.overallProgressPercentage
```

Assessment attempts, grading, project submissions, certificates, recommendations, and content snapshots remain unimplemented.

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
## Learner assessment attempts

Authenticated users can list published assessments for an enrolled, started course, start or resume an in-progress attempt, autosave selected answer-option IDs, submit for automatic grading, and review their own attempt history. Attempts belong to the current JWT user; the API never accepts a user ID. Cross-user and mismatched enrollment/course/assessment/attempt combinations return `404`.

The lifecycle is `InProgress` → `Passed` or `Failed`. Starting is idempotent while an in-progress attempt exists. A passed assessment cannot be retaken. `MaximumAttempts` counts both active and completed attempts; `null` allows unlimited attempts, and failed attempts may be retried while capacity remains.

Routes (all require a bearer token):

```text
GET  /api/me/career-enrollments/{enrollmentId}/courses/{courseId}/assessments
POST /api/me/career-enrollments/{enrollmentId}/courses/{courseId}/assessments/{assessmentId}/attempts
GET  /api/me/career-enrollments/{enrollmentId}/courses/{courseId}/assessments/{assessmentId}/attempts
GET  /api/me/career-enrollments/{enrollmentId}/courses/{courseId}/assessments/{assessmentId}/attempts/{attemptId}
PUT  /api/me/career-enrollments/{enrollmentId}/courses/{courseId}/assessments/{assessmentId}/attempts/{attemptId}/responses
POST /api/me/career-enrollments/{enrollmentId}/courses/{courseId}/assessments/{assessmentId}/attempts/{attemptId}/submit
```

Start has no request body. Autosave permits partial progress:

```powershell
$body = @{ responses = @(@{ questionId = '<question-guid>'; selectedAnswerOptionIds = @('<option-guid>') }) } | ConvertTo-Json -Depth 5
Invoke-RestMethod -Method Put -Uri "$baseUrl/api/me/career-enrollments/$enrollmentId/courses/$courseId/assessments/$assessmentId/attempts/$attemptId/responses" -Headers @{ Authorization = "Bearer $accessToken" } -ContentType 'application/json' -Body $body
```

Submission can use saved responses (`{}`) or atomically apply a final response batch in the same shape:

```powershell
Invoke-RestMethod -Method Post -Uri "$baseUrl/api/me/career-enrollments/$enrollmentId/courses/$courseId/assessments/$assessmentId/attempts/$attemptId/submit" -Headers @{ Authorization = "Bearer $accessToken" } -ContentType 'application/json' -Body $body
```

A completed result contains the score, points, pass/fail state, and per-response `isCorrect`/`pointsAwarded`. It never contains correct option IDs. Start and in-progress payloads expose prompts and choices but no correctness or answer-key metadata.

Questions award all points or zero: single-choice and true/false require their one correct option; multiple-choice requires an exact set match with no omission or extra choice. The score is `pointsEarned / totalPoints × 100`, rounded to two decimals, and the passing threshold is inclusive.

Course completion now requires every required lesson completed and every currently Published course assessment passed. Draft/Archived assessments and projects do not affect completion. Passing the final requirement recalculates course and enrollment completion.

Current limitations: choice-based questions only; no partial credit, written responses, timers, manual grading, project submissions, certificates, recommendations, notifications, or frontend.
## External learning resources

Careersity curates links and limited descriptive metadata for learning material owned and hosted by external educators, universities, and providers. It does not download, mirror, scrape, execute, or claim ownership of third-party content.

Administrators manage providers, instructors, and reusable URL resources through the `Admin External Learning` Swagger group. Supported resource types include lecture videos, playlists, readings, exercises, external assessments, assignments, answer guides, course websites, certificate opportunities, datasets, software tools, and other links. Access is described as Free, FreeWithAccount, AuditFree, Paid, InstitutionRestricted, or Unknown.

Providers, instructors, and resources use Draft → Published → Archived lifecycle states. Published instructional metadata is immutable. A Published resource may update only its server-generated `LastReviewedAtUtc` maintenance timestamp. Instructor publication requires a Published provider; resource publication requires a Published provider and, when assigned, a Published instructor belonging to that provider.

Draft courses can assign resources with an order, required flag, and optional notes. Assignments are immutable after course publication, and course publication requires every assignment to reference a Published resource. Public and learner endpoints expose only Published resources with attribution and the original URL.

Required resources use learner self-confirmation. Starting and completing are idempotent; completion never fetches or verifies the remote content. An `ExternalAssessment` is a URL resource and is not a native Careersity `Assessment`: it creates no attempt and receives no automatic grade.

Course progress is a simple count of completed required requirements divided by total required requirements. Requirements are required lessons, Published native assessments, and required Published external resources. Optional, Draft, and Archived resources do not block completion. If a required resource is archived before completion, it stops being a current requirement; historical progress remains stored.

Public routes:

```text
GET /api/learning-providers
GET /api/learning-providers/{slug}
GET /api/learning-providers/{providerId}/instructors
GET /api/external-learning-resources
GET /api/external-learning-resources/{resourceId}
GET /api/courses/{courseSlug}/external-resources
```

Administrator routes are under `/api/admin/learning-providers`, `/api/admin/instructors`, `/api/admin/external-learning-resources`, and `/api/admin/courses/{courseId}/external-resources`. Learner routes are under `/api/me/career-enrollments/{enrollmentId}/courses/{courseId}/external-resources` with `/start` and `/complete` actions.

PowerShell request examples:

```powershell
$provider = @{ name='Example University'; slug='example-university'; description='External provider'; websiteUrl='https://example.edu'; logoUrl=$null } | ConvertTo-Json
$resource = @{ learningProviderId='<provider-guid>'; instructorId=$null; title='Example lecture'; description='Hosted by the provider'; resourceType='LectureVideo'; accessType='Free'; url='https://example.edu/lecture'; sourceLabel='Example University'; estimatedDurationMinutes=45 } | ConvertTo-Json
$assignment = @{ externalLearningResourceId='<resource-guid>'; order=0; isRequired=$true; notes='Complete the hosted lecture.' } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri "$baseUrl/api/admin/learning-providers" -Headers @{ Authorization="Bearer $adminToken" } -ContentType 'application/json' -Body $provider
Invoke-RestMethod -Method Post -Uri "$baseUrl/api/admin/external-learning-resources" -Headers @{ Authorization="Bearer $adminToken" } -ContentType 'application/json' -Body $resource
Invoke-RestMethod -Method Post -Uri "$baseUrl/api/admin/courses/$courseId/external-resources" -Headers @{ Authorization="Bearer $adminToken" } -ContentType 'application/json' -Body $assignment
Invoke-RestMethod -Method Post -Uri "$baseUrl/api/me/career-enrollments/$enrollmentId/courses/$courseId/external-resources/$assignmentId/complete" -Headers @{ Authorization="Bearer $learnerToken" }
```

Current limitations include no provider APIs or OAuth, URL synchronization, scraping, availability jobs, automatic remote completion verification, file/video hosting, Careersity-issued certificates, project review, notifications, payments, recommendations, or frontend.
## MVP operations and deployment

The backend is a .NET 8 Clean Architecture API backed by PostgreSQL. Its MVP covers identity, administrator-authored career curricula, third-party learning-resource attribution, learner enrollment/progress, and optional native assessment grading. Careersity organizes publicly accessible learning links; it does not own external content or issue degrees.

Prerequisites are .NET 8 SDK, PostgreSQL 16, and optionally Docker Desktop. Configure `ConnectionStrings__CareersityDatabase`, `Jwt__Issuer`, `Jwt__Audience`, a random signing key of at least 32 bytes, and positive token lifetimes. Initial administrator provisioning remains opt-in through `InitialAdmin__Enabled`; credentials must come from environment variables or user secrets.

Database migrations never run implicitly by default. Set `Database__ApplyMigrationsOnStartup=true` only in Development or a controlled deployment. When enabled, migrations run before administrator provisioning and seed data. When disabled, `/health/ready` reports 503 if migrations are pending. Never use `EnsureCreated()`.

Development/demo seeding is disabled by default. Enable `SeedData__Enabled` and individual `IncludeDemoAdministrator`, `IncludeDemoLearner`, or `IncludeSampleCurriculum` flags. User credentials are configuration-only and existing password hashes are never replaced. Sample curriculum creates the Data Analyst pathway with three levels, nine concise courses, six skills, and Draft provider records. Provider names identify original sources and do not imply partnership or endorsement. Unreviewed external resources are not published.

Administrators can validate or atomically import nested JSON curricula:

```text
POST /api/admin/curriculum-imports/validate
POST /api/admin/curriculum-imports
```

Validation performs no writes and returns paths, codes, messages, warnings, and counts. Import repeats validation, uses one transaction, and supports only `CreateOnly`; existing or repeated slugs fail rather than merge. Requests are limited to 5 MB, 100 skills/providers, 200 instructors/courses, 2,000 lessons, 3,000 external resources, 20 levels, and 500 pathway assignments. URLs are format-validated but never fetched. See the request records under `Application/CurriculumImports/Requests` for the complete JSON shape.

`GET /health` is database-independent liveness. `GET /health/ready` checks configured PostgreSQL connectivity, applied migrations, and core service resolution. Neither endpoint requires authentication or exposes connection strings.

Every response includes `X-Correlation-ID`. A safe client value is preserved; otherwise one is generated and placed in the structured logging scope and Problem Details trace identifier. Request logs contain method, path, status, duration, correlation, and authenticated user ID when present—not bodies, authorization headers, passwords, tokens, imported documents, or connection strings.

CORS uses explicit `Cors__AllowedOrigins__N` entries. Production rejects missing/wildcard origins. Baseline `nosniff`, frame-denial, referrer, and API CSP headers are applied. Existing authentication rate limits remain unchanged.

Local containers use API port `58080` and PostgreSQL host port `55433`:

```powershell
Copy-Item .env.example .env
docker compose config
docker compose build
docker compose up -d
Invoke-WebRequest http://localhost:58080/health
Invoke-WebRequest http://localhost:58080/health/ready
```

The multi-stage API image uses official .NET 8 SDK/runtime images and the non-root runtime `app` user. `.env` is ignored; `.env.example` contains development placeholders only. GitHub Actions restores, builds, tests with Testcontainers, and builds the Docker image on pull requests and pushes to `main`; it does not deploy or publish images.

Production checklist:

- Use a dedicated PostgreSQL database/user and a randomly generated JWT key.
- Configure explicit HTTPS frontend CORS origins and reverse-proxy TLS.
- Decide whether the deployment job or controlled startup applies migrations.
- Keep seed and demo-user flags disabled.
- Provision the initial administrator through secret-backed environment configuration, then disable provisioning.
- Verify `/health`, `/health/ready`, correlation headers, and structured logs.
- Back up PostgreSQL regularly and test restoration before launch.
- Retain database backups before every migration-bearing release.

Known MVP limitations include no frontend, recommendations, certificates, payments, provider integrations, scraping, URL monitoring, email, notifications, distributed cache, message broker, or automatic production deployment.
