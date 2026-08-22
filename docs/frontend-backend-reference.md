# Careersity frontend–backend integration reference

> Authoritative integration guide generated from the backend source and tests on 2026-08-04. The exported runtime contract is [`careersity-openapi.json`](./careersity-openapi.json). Where Swagger is incomplete, this guide follows controllers, application contracts, services, domain rules, and integration tests.

## 1. Architecture and solution structure

```text
Domain
   ↑
Application
   ↑
Infrastructure
   ↑
API
```

The arrows show allowed higher-layer references to lower-layer abstractions/concepts, not UI inheritance. `Careersity.Domain` has no project references. `Careersity.Application` references Domain. `Careersity.Infrastructure` implements Application abstractions and references Application. `Careersity.Api` is the composition/HTTP layer and references Application and Infrastructure. Architecture tests enforce these boundaries. Controllers are transport adapters, **not Domain models**; frontend contracts are the request/response DTOs serialized by the API.

| Project | Responsibility, dependencies, and important content |
|---|---|
| `src/Careersity.Domain` | Entities, aggregates, guards, lifecycle methods, exceptions, and enums. Dependency-free. Folders: `Careers`, `Courses`, `Assessments`, `Identity`, `Learning`, `LearningResources`, `Projects`, `Skills`, `Enums`, `Common`. |
| `src/Careersity.Application` | Use cases and frontend-facing contracts. Depends only on Domain plus framework/packages. Folders by feature: `Identity`, `CareerCatalog`, `LearningContent`, `CurriculumActivities`, `LearningProgress`, `AssessmentAttempts`, `ExternalLearning`, `CurriculumImports`; each contains DTOs/requests/services/validators where applicable. `Abstractions` defines persistence/auth ports. |
| `src/Careersity.Infrastructure` | EF Core/PostgreSQL persistence, entity configurations/migrations, JWT/password/token implementations, startup migration/seeding/admin initialization. Depends on Application and Domain. |
| `src/Careersity.Api` | ASP.NET Core host, 24 controller classes, routing, authentication/authorization, Swagger, CORS, rate limits, Problem Details, correlation/security/logging middleware, and health checks. Depends on Application and Infrastructure. |
| `tests/Careersity.UnitTests` | Domain lifecycle/aggregate and application-service behavior, validators/DI, grader, import, auth, catalog, and progress tests. |
| `tests/Careersity.IntegrationTests` | PostgreSQL/Testcontainers persistence and migration tests plus HTTP tests for health, identity, catalog, content, curriculum activities, progress, ownership, and readiness. |

## 2. Runtime and environment

| Item | Value |
|---|---|
| Target framework | .NET 8 (`net8.0`) |
| PostgreSQL | PostgreSQL 16; Compose image `postgres:16-alpine` |
| Local launch-profile API | `http://localhost:5080`, `https://localhost:7080` |
| Docker Compose API | `http://localhost:58080` (container `8080`) |
| Docker Compose PostgreSQL | `localhost:55433` (container `5432`) |
| Swagger UI | `/swagger/index.html` in Development; e.g. `https://localhost:7080/swagger/index.html` |
| OpenAPI JSON | `/swagger/v1/swagger.json` in Development |
| Liveness | `GET /health` |
| Readiness | `GET /health/ready` |

Suggested Vite configuration:

```dotenv
VITE_API_BASE_URL=https://localhost:7080
```

Only `VITE_API_BASE_URL` belongs in browser-visible Vite configuration. Every `VITE_*` value is embedded in the client bundle; never put credentials or signing material there.

Backend variables relevant to frontend development are `ConnectionStrings__CareersityDatabase`, `Jwt__Issuer`, `Jwt__Audience`, `Jwt__SigningKey`, `Jwt__AccessTokenLifetimeMinutes`, `Jwt__RefreshTokenLifetimeDays`, `Cors__AllowedOrigins__0` (and numbered additional origins), `Database__ApplyMigrationsOnStartup`, `SeedData__Enabled`, `InitialAdmin__Enabled`, `InitialAdmin__Email`, `InitialAdmin__FirstName`, `InitialAdmin__LastName`, and `InitialAdmin__Password`. All remain backend-only. The frontend only needs the base URL and learns token expiry from authentication responses.

CORS uses an explicit origin list. For the normal Vite dev server set `Cors__AllowedOrigins__0=http://localhost:5173`. Allowed methods are `GET`, `POST`, `PUT`, `DELETE`; allowed headers are `Authorization`, `Content-Type`, `X-Correlation-ID`; `X-Correlation-ID` is exposed. Credentials are not enabled. Production refuses an empty origin list or `*`.

## 3. Authentication and profile contract

All JSON names below are exact camelCase. Protected requests use `Authorization: Bearer <accessToken>`.

| Method and route | Access | Request | Success | Important failures |
|---|---|---|---|---|
| `POST /api/auth/register` | Anonymous; 5/IP/hour | `RegisterRequest` | `201 AuthenticationResultDto` | `400` validation; `409` email exists; `429` |
| `POST /api/auth/login` | Anonymous; 10/IP/minute | `LoginRequest` | `200 AuthenticationResultDto` | generic `401` for nonexistent, inactive, or wrong password; `429` |
| `POST /api/auth/refresh` | Anonymous; 20/IP/minute | `RefreshAccessTokenRequest` | `200 AuthenticationResultDto` with rotated refresh token | generic `401` for unknown, expired, revoked, reused, or inactive-user token; `429` |
| `POST /api/auth/logout` | Authenticated | `LogoutRequest` | `204` | `401`; `400` validation. Unknown/already-revoked token is still `204` (idempotent). |
| `POST /api/auth/revoke-all` | Authenticated | none | `204` | `401` |
| `POST /api/auth/change-password` | Authenticated | `ChangeMyPasswordRequest` | `200 AuthenticationResultDto` | `400`; generic `401` for wrong current password. Revokes all sessions and issues a fresh pair. |
| `GET /api/users/me` | Authenticated | none | `200 UserProfileDto` | `401`; `404` if current user no longer exists |
| `PUT /api/users/me` | Authenticated | `UpdateMyProfileRequest` | `200 UserProfileDto` | `400`, `401`, `404` |

```ts
interface RegisterRequest { email: string; firstName: string; lastName: string; password: string; confirmPassword: string }
interface LoginRequest { email: string; password: string }
interface RefreshAccessTokenRequest { refreshToken: string }
interface LogoutRequest { refreshToken: string }
interface ChangeMyPasswordRequest { currentPassword: string; newPassword: string; confirmNewPassword: string }
interface UpdateMyProfileRequest { firstName: string; lastName: string }
interface AuthenticatedUserDto { id: string; email: string; firstName: string; lastName: string; fullName: string; role: UserRole }
interface AuthenticationResultDto { user: AuthenticatedUserDto; accessToken: string; accessTokenExpiresAtUtc: string; refreshToken: string; refreshTokenExpiresAtUtc: string }
interface UserProfileDto extends AuthenticatedUserDto { isActive: boolean; lastLoginAtUtc: string | null; createdAtUtc: string; updatedAtUtc: string | null }
```

Registration always creates `Learner`, records a login, and returns an access/refresh pair. Defaults are 15-minute access tokens and 14-day refresh tokens. Refresh rotates the refresh token: replace the stored token atomically. Reuse of an already revoked refresh token is treated as theft/replay and revokes every active refresh token for that user. Raw refresh tokens are returned only to the client; the database stores SHA-256 hashes.

Passwords are 10–128 characters and require uppercase, lowercase, digit, and non-alphanumeric characters. Registration email max is 320; names max 100. Login deliberately returns `"Invalid credentials or token."` for user enumeration resistance. Inactive users cannot login or refresh.

JWTs use HMAC-SHA256 and validate issuer, audience, signature, lifetime, and `ClockSkew = 0`. Claims are `sub` (user UUID), `email`, `given_name`, `family_name`, role claim serialized by JWT as the standard role claim but read with `MapInboundClaims=false`, and `jti`. Roles are exactly `Learner` and `Administrator`.

Frontend session behavior: on `401`, if an unexpired refresh token is available, coordinate all concurrent failures through one refresh promise, replace both tokens, and retry each original request once. An expired access token follows the same path. On failed refresh or revoked-token response, clear the session and return to login. Never refresh on `403`; it means the authenticated role lacks permission. Avoid redirect loops and never log tokens.

## 4. Authorization matrix

| Feature | Anonymous | Learner | Administrator | Notes |
|---|:---:|:---:|:---:|---|
| Health / readiness | ✓ | ✓ | ✓ | No authentication |
| Swagger (Development) | ✓ | ✓ | ✓ | UI availability is environment-gated |
| Public careers/courses/skills/providers/resources | ✓ | ✓ | ✓ | Published graph only |
| Registration / login / refresh | ✓ | ✓ | ✓ | Anonymous actions; authenticated callers are not blocked |
| Current user profile | — | ✓ | ✓ | `/api/users/me` |
| Learner enrollments/progress/attempts | — | ✓ | ✓ | `/api/me/**`; always current-user ownership |
| Admin career/course/resource management | — | — | ✓ | `AdministratorOnly` |
| Curriculum import | — | — | ✓ | Also mutation-rate-limited |

Every `/api/admin/**` controller requires the `Administrator` role. Every `/api/me/**` route requires authentication and derives user identity from `sub`; there is no user ID override. Therefore an Administrator can operate only on their own learner records via `/api/me/**`. Enrollment and attempt queries include current user ID, so wrong ownership generally returns `404`, intentionally hiding existence.

## 5. Global API conventions

### JSON and time

ASP.NET Core serializes properties as camelCase. API-visible enums use exact strings, not numbers. `DateTimeOffset` values serialize as ISO-8601 UTC timestamps (fields ending in `AtUtc`); parse them as instants. Nullable DTO properties appear as JSON `null` under the default serializer (nulls are not globally suppressed).

### Pagination and queries

```ts
interface PagedResult<T> { items: T[]; page: number; pageSize: number; totalCount: number }
```

There is **no `totalPages` field**; calculate `Math.ceil(totalCount / pageSize)`. Pages are 1-based. Default page is 1, default page size 20, maximum 100. Inputs below 1 are normalized (page → 1, pageSize clamped to 1); values over 100 are clamped.

Common `page`, `pageSize`, and `search` are server-side. Public filters: careers `categoryId`; courses `difficulty`, `skillId`; skills `category`. Admin list filters additionally include `status`; courses/assessments/projects may include `courseId`, `difficulty`, `skillId`, or `submissionType` as shown in the admin inventory. Enrollment list accepts `status`, `includeHistory`, `page`, `pageSize`. With no status and `includeHistory=false`, only Active/Paused enrollments appear; `includeHistory=true` includes all. Search is trimmed, case-insensitive substring matching; career/course search covers title plus short description, while most other admin search covers name/title. Debounce it.

### Correlation

Send optional `X-Correlation-ID` containing 1–128 characters from `[A-Za-z0-9._-]`. Invalid/missing values are replaced with a lowercase 32-hex GUID. The same header is returned and equals Problem Details `traceId`. Retain it in client diagnostics and support screens.

### Rate limiting

Only explicitly decorated routes are limited: register 5/IP/hour, login 10/IP/minute, refresh 20/IP/minute, and both curriculum-import routes 30/user-or-IP/minute. Queue size is zero. Rejection is `429 application/problem+json` with title `Too many requests`. The implementation does not explicitly add `Retry-After`; do not assume it exists. Other routes, including logout and normal mutations, are currently excluded despite the policy name `MutationRateLimit`.

### Errors and validation

```json
{
  "type": "https://httpstatuses.com/400",
  "title": "Request validation failed",
  "status": 400,
  "detail": "Validation failed: ...",
  "instance": "/api/...",
  "traceId": "client-or-server-correlation-id",
  "errors": { "Email": ["'Email' must not be empty."] }
}
```

`errors` exists only for FluentValidation exceptions. Its keys are FluentValidation/C# property paths (typically PascalCase, and possibly nested/indexed), **not guaranteed camelCase**. Map keys case-insensitively to form fields. Model-binding errors generated before the action may use ASP.NET Core's own validation Problem Details shape; clients must tolerate optional fields.

| Status | Meaning |
|---:|---|
| 400 | FluentValidation, argument, request/model validation |
| 401 | missing/invalid bearer token, invalid credentials/token, current-user auth failure |
| 403 | authenticated but policy/role denied |
| 404 | missing resource, non-Published public resource, or wrong ownership |
| 409 | application conflict or any Domain lifecycle/rule conflict |
| 429 | explicit rate-limit policy exceeded |
| 503 | persistence absent/unavailable through the unavailable context |
| 500 | unexpected; detail is suppressed to `An unexpected error occurred.` |

Database exception details are not intentionally exposed by the exception handler; unexpected exceptions receive the generic 500 detail. Draft/Archived resources queried through public endpoints generally produce 404. Display a friendly message plus `traceId` in expandable support details, never a raw stack trace.

```ts
// Documentation example; tolerate extra RFC 7807 extensions.
interface ApiProblem {
  type: string;
  title: string;
  status: number;
  detail?: string;
  instance?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
}
function isApiProblem(value: object): value is ApiProblem {
  return "type" in value && "title" in value && "status" in value;
}
```

## 6. Enum catalog

| Type | Exact strings | Used by |
|---|---|---|
| `ContentStatus` | `Draft`, `Published`, `Archived` | all publishable catalog/admin DTOs and admin status filters |
| `CourseDifficulty` | `Foundation`, `Beginner`, `Intermediate`, `Advanced` | courses/pathway/progress and filters |
| `LessonContentType` | `Article`, `Video`, `Exercise`, `ExternalResource`, `Mixed` | lessons/import |
| `QuestionType` | `SingleChoice`, `MultipleChoice`, `TrueFalse` | assessment authoring/attempt questions |
| `SkillCategory` | `Technical`, `Analytical`, `Communication`, `Professional`, `DomainKnowledge`, `Tool` | skills and filters |
| `SkillProficiencyLevel` | `Awareness`, `Beginner`, `Intermediate`, `Advanced` | career/course skill relations/import |
| `ProjectSubmissionType` | `RepositoryUrl`, `PortfolioUrl`, `DocumentUrl`, `VideoUrl`, `TextResponse`, `Mixed` | projects/admin filter |
| `UserRole` | `Learner`, `Administrator` | user/auth DTOs and authorization |
| `EnrollmentStatus` | `Active`, `Paused`, `Completed`, `Withdrawn` | enrollment lifecycle/filter |
| `AssessmentAttemptStatus` | `InProgress`, `Passed`, `Failed` | attempt DTOs |
| `ExternalResourceType` | `LectureVideo`, `VideoPlaylist`, `Reading`, `Exercise`, `ExternalAssessment`, `Assignment`, `AnswerGuide`, `CourseWebsite`, `CertificateOpportunity`, `Dataset`, `SoftwareTool`, `Other` | external resources/import/progress |
| `ResourceAccessType` | `Free`, `FreeWithAccount`, `AuditFree`, `Paid`, `InstitutionRestricted`, `Unknown` | external resources/import |
| `CourseAvailabilityStatus` | `Locked`, `Available`, `InProgress`, `Completed` | learner course progress only |

## 7. Public catalog endpoints

All routes in this section are anonymous and return only Published content and required Published parents/relations.

| Route | Query | Response, sorting, empty/not-found behavior |
|---|---|---|
| `GET /api/career-categories` | none | `CareerCategoryDto[]`, name ascending; empty `[]` |
| `GET /api/careers` | `page`, `pageSize`, `search`, `categoryId` | `PagedResult<CareerListItemDto>`, title ascending; empty page items; career and category must be Published |
| `GET /api/careers/{slug}` | slug string | `CareerDetailDto`; `404` if career/category hidden; skills and primary pathway filtered to Published |
| `GET /api/careers/{careerId}/pathway` | career UUID | `CareerPathwayDto`; `404` unless career and primary pathway are Published |
| `GET /api/skills` | `page`, `pageSize`, `search`, `category` | `PagedResult<SkillListItemDto>`, name ascending; empty page items |
| `GET /api/skills/{slug}` | slug | `SkillDto`; `404` for missing/Draft/Archived |
| `GET /api/courses` | `page`, `pageSize`, `search`, `difficulty`, `skillId` | `PagedResult<CourseListItemDto>`, title ascending |
| `GET /api/courses/{slug}` | slug | `CourseDetailDto`; lesson **summaries**, Published prerequisite/skill relations; `404` if hidden |
| `GET /api/courses/{courseSlug}/lessons/{lessonSlug}` | slugs | full `LessonDto` including `content`; course must be Published; `404` otherwise |
| `GET /api/courses/{courseSlug}/external-resources` | slug | `PublicExternalResourceDto[]`, assignment order; course/resource/provider and optional instructor must be Published |
| `GET /api/courses/{courseSlug}/assessments` | slug | `PublicAssessmentSummaryDto[]`, title ascending; no questions/answers/correctness |
| `GET /api/courses/{courseSlug}/projects` | slug | `PublicProjectSummaryDto[]`, title ascending |
| `GET /api/courses/{courseSlug}/projects/{projectId}` | slug + UUID | `PublicProjectDetailDto`; `404` unless project belongs to Published course and is Published |
| `GET /api/learning-providers` | none | `LearningProviderDto[]`, name ascending; not paged |
| `GET /api/learning-providers/{slug}` | slug | `LearningProviderDto`; `404` hidden/missing |
| `GET /api/learning-providers/{providerId}/instructors` | provider UUID | `InstructorDto[]`, name ascending; provider and instructors Published; hidden provider → `404` |
| `GET /api/external-learning-resources` | none | `ExternalLearningResourceListItemDto[]`, title ascending; not paged; published dependency graph only |
| `GET /api/external-learning-resources/{resourceId}` | UUID | `ExternalLearningResourceDetailDto`; `instructor` nullable; hidden dependency → `404` |

Representative requests:

```http
GET /api/careers?page=1&pageSize=20&search=data&categoryId=<uuid>
GET /api/courses?page=1&pageSize=20&difficulty=Beginner&skillId=<uuid>
GET /api/skills?page=1&pageSize=20&category=Technical
```

Representative career response (nullable text/times may be `null`):

```json
{
  "id": "uuid", "careerCategoryId": "uuid", "categoryName": "Technology",
  "title": "Data Analyst", "slug": "data-analyst", "shortDescription": "...",
  "estimatedDurationWeeks": 24, "status": "Published"
}
```

Exact public DTO fields:

- `CareerCategoryDto`: `id`, `name`, `slug`, `description?`, `status`, `createdAtUtc`, `updatedAtUtc?`.
- `CareerListItemDto`: `id`, `careerCategoryId`, `categoryName`, `title`, `slug`, `shortDescription`, `estimatedDurationWeeks?`, `status`.
- `CareerSkillDto`: `id`, `skillId`, `skillName`, `skillSlug`, `skillCategory`, `requiredProficiencyLevel`, `isRequired`, `displayOrder`.
- `PathwayLevelCourseDto`: `id`, `courseId`, `courseTitle`, `courseSlug`, `difficulty`, `estimatedDurationMinutes`, `order`, `isRequired`.
- `PathwayLevelDto`: `id`, `name`, `description?`, `order`, `courses`.
- `CareerPathwayDto`: `id`, `careerId`, `name`, `description?`, `version`, `isPrimary`, `status`, `levels`.
- `CareerDetailDto`: `id`, `careerCategory`, `title`, `slug`, `shortDescription`, `detailedDescription?`, `responsibilities?`, `estimatedDurationWeeks?`, `status`, `skills`, `primaryPathway?`, `createdAtUtc`, `updatedAtUtc?`.
- `SkillListItemDto`: `id`, `name`, `slug`, `category`, `status`; `SkillDto` adds `description?`, `createdAtUtc`, `updatedAtUtc?`.
- `CourseListItemDto`: `id`, `title`, `slug`, `shortDescription`, `difficulty`, `estimatedDurationMinutes`, `status`, `lessonCount`, `skillCount`.
- `LessonSummaryDto`: `id`, `title`, `slug`, `summary?`, `contentType`, `estimatedDurationMinutes`, `order`, `isRequired`.
- `LessonDto`: summary fields plus `courseId`, `content?`, `externalResourceUrl?`, `createdAtUtc`, `updatedAtUtc?`.
- `CoursePrerequisiteDto`: `id`, `prerequisiteCourseId`, `prerequisiteCourseTitle`, `prerequisiteCourseSlug`, `isRequired`.
- `CourseSkillDto`: `id`, `skillId`, `skillName`, `skillSlug`, `skillCategory`, `proficiencyLevel`, `isPrimary`.
- `CourseDetailDto`: `id`, `title`, `slug`, `shortDescription`, `detailedDescription?`, `difficulty`, `estimatedDurationMinutes`, `status`, `lessons`, `prerequisites`, `skills`, `createdAtUtc`, `updatedAtUtc?`.
- `PublicAssessmentSummaryDto`: `id`, `title`, `description?`, `passingScorePercentage`, `maximumAttempts?`, `questionCount`, `totalPoints`.
- `PublicProjectSummaryDto`: `id`, `title`, `description`, `submissionType`, `estimatedDurationMinutes`.
- `PublicProjectDetailDto`: summary fields plus `courseId`, `courseTitle`, `courseSlug`, `instructions`, `expectedOutput?`, `evaluationCriteria?`.
- `LearningProviderDto`: `id`, `name`, `slug`, `description?`, `websiteUrl?`, `logoUrl?`, `status`, `createdAtUtc`, `updatedAtUtc?`.
- `InstructorDto`: `id`, `learningProviderId`, `providerName`, `name`, `title?`, `biography?`, `profileUrl?`, `status`, `createdAtUtc`, `updatedAtUtc?`.
- `ExternalLearningResourceListItemDto`: `id`, `title`, `resourceType`, `accessType`, `url`, `providerId`, `providerName`, `instructorId?`, `instructorName?`, `estimatedDurationMinutes?`, `lastReviewedAtUtc?`, `status`.
- `ExternalLearningResourceDetailDto`: `id`, `title`, `description?`, `resourceType`, `accessType`, `url`, `sourceLabel?`, `provider`, `instructor?`, `estimatedDurationMinutes?`, `lastReviewedAtUtc?`, `status`, `createdAtUtc`, `updatedAtUtc?`.
- `PublicExternalResourceDto`: `resourceId`, `title`, `description?`, `resourceType`, `accessType`, `url`, `sourceLabel?`, `providerName`, `instructorName?`, `estimatedDurationMinutes?`, `order`, `isRequired`.

```text
Career
├── Category
├── Skills
└── Primary Pathway
    └── Levels (order ascending)
        └── Courses (order ascending)
```

Course detail deliberately avoids large lesson bodies; fetch the dedicated lesson route when opened. Correct assessment answers are never public. `ExternalAssessment` is a third-party resource, is not Careersity-graded, and creates no `AssessmentAttempt`. The backend returns URLs but never fetches them. Provider/instructor/resource records are attribution/catalog references, not partnership claims. `lastReviewedAtUtc` is maintenance metadata. Open external URLs outside Careersity with appropriate browser security (`noopener,noreferrer`).

## 8. Learner enrollment and progress

All routes require a valid bearer token, use the current `sub`, and return `404` for another user's IDs.

### Enrollment routes

| Route | Request / response | Behavior and statuses |
|---|---|---|
| `POST /api/me/career-enrollments` | `{ "careerId": "uuid" }` → `201 CareerEnrollmentDetailDto` | Career/category, primary pathway, and all pathway courses must be Published; pathway needs a required course. `404` hidden career; `409` invalid graph or duplicate non-Withdrawn enrollment. |
| `GET /api/me/career-enrollments` | query `status?`, `includeHistory=false`, `page=1`, `pageSize=20` → paged list | Updated/enrolled descending. Default Active/Paused only. |
| `GET /api/me/career-enrollments/{enrollmentId}` | `CareerEnrollmentDetailDto` | `404` missing/wrong owner. |
| `POST .../{enrollmentId}/pause` | no body → `204` | Only Active; otherwise `409`. |
| `POST .../{enrollmentId}/resume` | no body → `204` | Only Paused; otherwise `409`. |
| `POST .../{enrollmentId}/withdraw` | no body → `204` | Only Active/Paused; otherwise `409`. |

Duplicate enrollment is defined per pathway: any Active, Paused, or Completed enrollment blocks another (`409`). A Withdrawn enrollment does not, so re-enrollment creates a new durable record. Completed cannot pause/resume/withdraw. Learning mutation requires Active; read operations remain available for history.

DTOs:

- `CurrentCourseDto`: `courseId`, `title`, `slug`, `pathwayLevelName`, `pathwayLevelOrder`, `courseOrder`, `progressPercentage`.
- `CareerEnrollmentListItemDto`: `id`, `careerId`, `careerTitle`, `careerSlug`, `careerPathwayId`, `pathwayName`, `pathwayVersion`, `status`, `enrolledAtUtc`, `startedAtUtc?`, `completedAtUtc?`, `overallProgressPercentage`, `completedCourseCount`, `totalRequiredCourseCount`, `currentCourse?`.
- `CareerEnrollmentDetailDto`: `id`, `career` (`id`,`title`,`slug`), `pathway` (`id`,`name`,`version`), `status`, `enrolledAtUtc`, `startedAtUtc?`, `pausedAtUtc?`, `completedAtUtc?`, `withdrawnAtUtc?`, `overallProgressPercentage`, `completedRequiredCourseCount`, `totalRequiredCourseCount`, `levels`.
- `LearnerPathwayLevelDto`: `id`, `name`, `description?`, `order`, `isCompleted`, `progressPercentage`, `courses`.
- `LearnerCourseProgressDto`: `courseId`, `title`, `slug`, `difficulty`, `estimatedDurationMinutes`, `order`, `isRequired`, `availabilityStatus`, `isStarted`, `isCompleted`, `progressPercentage`, `completedLessonCount`, `totalRequiredLessonCount`, `startedAtUtc?`, `completedAtUtc?`, `completedRequiredExternalResourceCount`, `totalRequiredExternalResourceCount`.

### Course and lesson routes

| Route | Response |
|---|---|
| `GET /api/me/career-enrollments/{enrollmentId}/courses/{courseId}` | `200 LearnerCourseDetailDto` |
| `POST .../{enrollmentId}/courses/{courseId}/start` | `200 LearnerCourseDetailDto` |
| `POST .../{enrollmentId}/courses/{courseId}/complete` | `200 LearnerCourseDetailDto` or `409` unmet requirement |
| `GET .../{enrollmentId}/courses/{courseId}/lessons/{lessonId}` | `200 LearnerLessonDetailDto` |
| `POST .../lessons/{lessonId}/start` | `200 LearnerLessonDetailDto` |
| `POST .../lessons/{lessonId}/complete` | `200 LearnerLessonDetailDto` |

`LearnerCourseDetailDto` fields: `courseId`, `title`, `slug`, `shortDescription`, `detailedDescription?`, `difficulty`, `estimatedDurationMinutes`, `availabilityStatus`, `isStarted`, `isCompleted`, `progressPercentage`, `prerequisites`, `lessons`, `projects`, `assessmentSummaries`, `externalResources?`, `completedRequiredExternalResourceCount`, `totalRequiredExternalResourceCount`.

`LearnerLessonProgressDto` fields: `lessonId`, `title`, `slug`, `summary?`, `contentType`, `estimatedDurationMinutes`, `order`, `isRequired`, `isStarted`, `isCompleted`, `startedAtUtc?`, `completedAtUtc?`. `LearnerLessonDetailDto` adds `courseId`, `content?`, `externalResourceUrl?`.

Availability rules:

1. A course outside the enrolled pathway is `404`.
2. Completed stays `Completed`; an existing incomplete progress record is `InProgress`.
3. An unstarted non-Published course is `Locked` and cannot receive new activity.
4. Every required course in every **earlier pathway level** must be complete.
5. Every explicit required course prerequisite must be complete.
6. Same-level `order` affects display, not locking. Optional earlier courses do not block.
7. Otherwise the course is `Available`.

Start/lesson-start/lesson-complete are effectively idempotent: existing records are reused and access is recorded; complete remains complete. An explicit course complete returns `409` if requirements remain. Completing any lesson, passing an assessment, or completing an external resource reevaluates completion automatically. Paused/Withdrawn/Completed enrollments reject new activity with `409`. An already-started archived course can be read, but explicit activity checks are inconsistent by operation: course/lesson mutations require Published, and new archived content progress is rejected. Treat Archived as read-only history.

Completion formula uses three sets:

```text
requirements =
  all required lessons (regardless of lesson status; lessons have no ContentStatus)
  + all Published native assessments
  + all required assignments whose external resource is Published

course is complete iff every item in all three sets is complete/passed.
progressPercentage = completed requirement count / total requirement count * 100
```

The displayed course percentage combines required lessons, Published assessments, and required Published external resources. A zero-requirement denominator yields 0 in projection logic. Enrollment progress is completed required pathway courses / total required pathway courses × 100. Enrollment automatically becomes Completed when all required pathway courses complete.

### External-resource progress

| Route | Response |
|---|---|
| `GET .../{enrollmentId}/courses/{courseId}/external-resources` | `LearnerExternalResourceProgressDto[]`, assignment order |
| `POST .../external-resources/{assignmentId}/start` | `LearnerExternalResourceProgressDto` |
| `POST .../external-resources/{assignmentId}/complete` | `LearnerExternalResourceProgressDto` |

Fields: `assignmentId`, `resourceId`, `title`, `description?`, `resourceType`, `accessType`, `url`, `providerName`, `instructorName?`, `order`, `isRequired`, `isStarted`, `isCompleted`, `startedAtUtc?`, `completedAtUtc?`, `estimatedDurationMinutes?`.

Completion is learner self-confirmed. Start and complete reuse existing progress and are idempotent. Course must first be started; enrollment must be Active; only Published resource assignments are listed/usable. Required resources block course completion; optional ones do not. `ExternalAssessment` remains self-confirmed and never creates a native attempt. Archived resources cannot receive new progress.

### Native assessment attempts

| Route | Request | Success response |
|---|---|---|
| `GET .../{courseId}/assessments` | none | `LearnerAssessmentSummaryDto[]` |
| `POST .../{courseId}/assessments/{assessmentId}/attempts` | none | `201 AssessmentAttemptStartDto` when created; `200` when existing InProgress attempt reused |
| `GET .../{assessmentId}/attempts` | none | `AssessmentAttemptHistoryItemDto[]`, attempt number descending |
| `GET .../{assessmentId}/attempts/{attemptId}` | none | `AssessmentAttemptDetailDto` |
| `PUT .../{attemptId}/responses` | `SaveAssessmentResponsesRequest` | `AssessmentAttemptDetailDto` (autosave) |
| `POST .../{attemptId}/submit` | `SubmitAssessmentAttemptRequest` | `AssessmentAttemptDetailDto` |

DTO fields:

- `LearnerAssessmentSummaryDto`: `assessmentId`, `title`, `description?`, `passingScorePercentage`, `maximumAttempts?`, `attemptsUsed`, `attemptsRemaining?`, `hasPassed`, `activeAttemptId?`, `questionCount`, `totalPoints`.
- `AssessmentAttemptStartDto`: `attemptId`, `assessmentId`, `attemptNumber`, `status`, `startedAtUtc`, `questions`.
- `LearnerAssessmentQuestionDto`: `questionId`, `prompt`, `questionType`, `order`, `points`, `answerOptions`.
- `LearnerAnswerOptionDto`: `answerOptionId`, `text`, `order`.
- `LearnerAssessmentResponseDto`: `questionId`, `selectedAnswerOptionIds`, `isCorrect?`, `pointsAwarded?`.
- `AssessmentAttemptDetailDto`: `attemptId`, `assessmentId`, `assessmentTitle`, `attemptNumber`, `status`, `startedAtUtc`, `submittedAtUtc?`, `scorePercentage?`, `pointsEarned?`, `totalPoints?`, `passed`, `responses`.
- `AssessmentAttemptHistoryItemDto`: `attemptId`, `attemptNumber`, `status`, `startedAtUtc`, `submittedAtUtc?`, `scorePercentage?`, `passed`.
- `SaveAssessmentResponsesRequest`: `{ responses: Array<{ questionId, selectedAnswerOptionIds }> }`.
- `SubmitAssessmentAttemptRequest`: `{ responses?: Array<{ questionId, selectedAnswerOptionIds }> | null }`.

The course must be started, enrollment Active for mutations, and course/assessment Published. Starting reuses the single InProgress attempt. `maximumAttempts=null` means unlimited; otherwise all attempts count. A passed assessment cannot restart; exhausted attempts return `409`. Autosave may save partial/empty selections and upserts responses. Submission requires every question and exactly one answer for SingleChoice/TrueFalse, at least one for MultipleChoice. MultipleChoice uses exact-set grading with no partial credit. Score is `round(pointsEarned * 100 / totalPoints, 2, away-from-zero)`; `score >= passingScorePercentage` passes.

Before submission, `isCorrect` and `pointsAwarded` remain null. After submission they may reveal correctness for the learner's response, but **correct option IDs are never returned**. Re-submitting an already finished attempt returns its existing result (idempotent). A pass reevaluates automatic course/enrollment completion.

## 9. Administrator endpoint inventory

Every route below requires `AdministratorOnly`. Create returns `201` plus the corresponding detail DTO. Get/list/update return `200`. Publish/archive/reorder/delete normally return `204`. Common failures: `400` validation, `401`, `403`, `404`, and `409` lifecycle/dependency/uniqueness conflict. Unless called out, request/response names match the listed contract.

### Career Categories

| Method/path | Body → result |
|---|---|
| `POST /api/admin/career-categories` | `CreateCareerCategoryRequest` → `CareerCategoryDto` |
| `GET /api/admin/career-categories` | none → `CareerCategoryDto[]` |
| `GET /api/admin/career-categories/{id}` | none → `CareerCategoryDto` |
| `PUT /api/admin/career-categories/{id}` | `UpdateCareerCategoryRequest` → `CareerCategoryDto` |
| `POST /api/admin/career-categories/{id}/publish` | none → `204` |
| `POST /api/admin/career-categories/{id}/archive` | none → `204` |

### Careers and Career Skills

| Method/path | Body/query → result |
|---|---|
| `POST /api/admin/careers` | `CreateCareerRequest` → `CareerDetailDto` |
| `GET /api/admin/careers` | `page,pageSize,search,categoryId,status` → paged `CareerListItemDto` |
| `GET /api/admin/careers/{id}` | → `CareerDetailDto` |
| `PUT /api/admin/careers/{id}` | `UpdateCareerRequest` → `CareerDetailDto` |
| `PUT /api/admin/careers/{id}/category` | `ChangeCareerCategoryRequest` → `204` |
| `POST /api/admin/careers/{id}/publish` | → `204` |
| `POST /api/admin/careers/{id}/archive` | → `204` |
| `GET /api/admin/careers/{careerId}/skills` | → `CareerSkillDto[]` |
| `POST /api/admin/careers/{careerId}/skills` | `AssignCareerSkillRequest` → `CareerSkillDto` (`201`) |
| `PUT /api/admin/careers/{careerId}/skills/{careerSkillId}` | `UpdateCareerSkillRequest` → `CareerSkillDto` |
| `DELETE /api/admin/careers/{careerId}/skills/{careerSkillId}` | → `204` |

### Career Pathways

| Method/path | Body → result |
|---|---|
| `POST /api/admin/careers/{careerId}/pathways` | `CreateCareerPathwayRequest` → `CareerPathwayDto` |
| `GET /api/admin/careers/{careerId}/pathways` | → `CareerPathwayDto[]` |
| `GET /api/admin/careers/{careerId}/pathways/{pathwayId}` | → `CareerPathwayDto` |
| `PUT /api/admin/careers/{careerId}/pathways/{pathwayId}` | `UpdateCareerPathwayRequest` → `CareerPathwayDto` |
| `POST .../{pathwayId}/publish` / `archive` | none → `204` |
| `POST .../{pathwayId}/levels` | `AddPathwayLevelRequest` → `PathwayLevelDto` (`201`) |
| `PUT .../{pathwayId}/levels/reorder` | `ReorderPathwayLevelsRequest` → `204` |
| `PUT .../{pathwayId}/levels/{levelId}` | `UpdatePathwayLevelRequest` → `PathwayLevelDto` |
| `DELETE .../{pathwayId}/levels/{levelId}` | → `204` |
| `POST .../{pathwayId}/levels/{levelId}/courses` | `AddPathwayLevelCourseRequest` → `PathwayLevelCourseDto` (`201`) |
| `PUT .../{pathwayId}/levels/{levelId}/courses/reorder` | `ReorderPathwayCoursesRequest` → `204` |
| `PUT .../{pathwayId}/levels/{levelId}/courses/{assignmentId}` | `UpdatePathwayLevelCourseRequest` → `PathwayLevelCourseDto` |
| `DELETE .../{pathwayId}/levels/{levelId}/courses/{assignmentId}` | → `204` |

### Skills

| Method/path | Body/query → result |
|---|---|
| `POST /api/admin/skills` | `CreateSkillRequest` → `SkillDto` |
| `GET /api/admin/skills` | `page,pageSize,search,category,status` → paged `SkillListItemDto` |
| `GET /api/admin/skills/{id}` | → `SkillDto` |
| `PUT /api/admin/skills/{id}` | `UpdateSkillRequest` → `SkillDto` |
| `POST /api/admin/skills/{id}/publish` / `archive` | → `204` |

### Courses, Lessons, Prerequisites, and Course Skills

| Method/path | Body/query → result |
|---|---|
| `POST /api/admin/courses` | `CreateCourseRequest` → `CourseDetailDto` |
| `GET /api/admin/courses` | `page,pageSize,search,difficulty,status,skillId` → paged `CourseListItemDto` |
| `GET /api/admin/courses/{id}` | → `CourseDetailDto` |
| `PUT /api/admin/courses/{id}` | `UpdateCourseRequest` → `CourseDetailDto` |
| `POST /api/admin/courses/{id}/publish` / `archive` | → `204` |
| `POST /api/admin/courses/{courseId}/lessons` | `AddLessonRequest` → `LessonDto` |
| `GET .../lessons/{lessonId}` | → `LessonDto` |
| `PUT .../lessons/{lessonId}` | `UpdateLessonRequest` → `LessonDto` |
| `DELETE .../lessons/{lessonId}` | → `204` |
| `PUT .../lessons/reorder` | `ReorderLessonsRequest` → `204` |
| `POST .../{courseId}/prerequisites` | `AddCoursePrerequisiteRequest` → `CoursePrerequisiteDto` |
| `PUT .../prerequisites/{prerequisiteId}` | `UpdateCoursePrerequisiteRequest` → `CoursePrerequisiteDto` |
| `DELETE .../prerequisites/{prerequisiteId}` | → `204` |
| `POST .../{courseId}/skills` | `AddCourseSkillRequest` → `CourseSkillDto` |
| `PUT .../skills/{courseSkillId}` | `UpdateCourseSkillRequest` → `CourseSkillDto` |
| `DELETE .../skills/{courseSkillId}` | → `204` |

### Assessments, Questions, and Answer Options

| Method/path | Body/query → result |
|---|---|
| `POST /api/admin/assessments` | `CreateAssessmentRequest` → `AssessmentAdminDetailDto` |
| `GET /api/admin/assessments` | `page,pageSize,search,courseId,status` → paged `AssessmentListItemDto` |
| `GET /api/admin/assessments/{assessmentId}` | → `AssessmentAdminDetailDto` |
| `PUT /api/admin/assessments/{assessmentId}` | `UpdateAssessmentRequest` → detail |
| `POST .../{assessmentId}/publish` / `archive` | → `204` |
| `POST .../{assessmentId}/questions` | `AddQuestionRequest` → `QuestionAdminDto` |
| `GET .../questions/{questionId}` | → `QuestionAdminDto` |
| `PUT .../questions/{questionId}` | `UpdateQuestionRequest` → question |
| `DELETE .../questions/{questionId}` | → `204` |
| `PUT .../questions/reorder` | `ReorderQuestionsRequest` → `204` |
| `POST .../questions/{questionId}/answer-options` | `AddAnswerOptionRequest` → `AnswerOptionAdminDto` |
| `PUT .../answer-options/{answerOptionId}` | `UpdateAnswerOptionRequest` → option |
| `DELETE .../answer-options/{answerOptionId}` | → `204` |
| `PUT .../answer-options/reorder` | `ReorderAnswerOptionsRequest` → `204` |

### Projects

| Method/path | Body/query → result |
|---|---|
| `POST /api/admin/projects` | `CreateProjectRequest` → `ProjectAdminDetailDto` |
| `GET /api/admin/projects` | `page,pageSize,search,courseId,submissionType,status` → paged `ProjectListItemDto` |
| `GET /api/admin/projects/{projectId}` | → `ProjectAdminDetailDto` |
| `PUT /api/admin/projects/{projectId}` | `UpdateProjectRequest` → detail |
| `POST .../{projectId}/publish` / `archive` | → `204` |

### Providers, Instructors, Resources, and Assignments

| Method/path | Body/query → result |
|---|---|
| `POST /api/admin/learning-providers` | `CreateLearningProviderRequest` → `LearningProviderDto` |
| `GET /api/admin/learning-providers` | `page,pageSize,search,status` → paged provider |
| `GET/PUT /api/admin/learning-providers/{id}` | none / `UpdateLearningProviderRequest` → provider |
| `POST .../{id}/publish` / `archive` | → `204` |
| `POST /api/admin/instructors` | `CreateInstructorRequest` → `InstructorDto` |
| `GET /api/admin/instructors` | `page,pageSize,search,status` → paged instructor |
| `GET/PUT /api/admin/instructors/{id}` | none / `UpdateInstructorRequest` → instructor |
| `PUT .../{id}/provider` | `ChangeInstructorProviderRequest` → instructor |
| `POST .../{id}/publish` / `archive` | → `204` |
| `POST /api/admin/external-learning-resources` | `CreateExternalLearningResourceRequest` → resource detail |
| `GET /api/admin/external-learning-resources` | `page,pageSize,search,status` → paged list item |
| `GET/PUT .../{id}` | none / `UpdateExternalLearningResourceRequest` → detail |
| `POST .../{id}/publish` / `archive` / `mark-reviewed` | → `204` |
| `POST /api/admin/courses/{courseId}/external-resources` | `AssignExternalResourceToCourseRequest` → `CourseExternalResourceDto` |
| `GET .../{courseId}/external-resources` | → `CourseExternalResourceDto[]` |
| `PUT .../external-resources/{assignmentId}` | `UpdateCourseExternalResourceRequest` → assignment |
| `DELETE .../external-resources/{assignmentId}` | → `204` |
| `PUT .../external-resources/reorder` | `ReorderCourseExternalResourcesRequest` → `204` |

Lifecycle: entities start Draft. Course/lesson/relationship, skill, pathway, assessment/question/option, project, provider, instructor, and external-resource updates are Draft-only through service/domain guards; publication repeated on a publishable entity may be idempotent, and archive is idempotent. **Actual exception:** career categories, careers, and career-skill assignments currently lack a Draft-only update guard and can be changed while Published/Archived. Treat this as a backend-contract risk, not permission to depend on mutable published content. Archived cannot be published directly or return to Draft. Career publication needs a Published category. Course publication requires ≥1 required lesson, ≥1 skill and primary skill, all associated skills/prerequisites/external resources Published, and unique lesson orders/slugs. Instructor publication needs Published provider. Resource publication needs Published provider and, when present, Published instructor belonging to it. Pathway publication requires a Published career, nonempty levels, unique orders, at least one required course, and Published assigned courses; pathway editing is Draft-only. Published assessments/projects and their children are immutable; publish rules validate valid questions/options/points as implemented. Public visibility may disappear when a required parent is archived even if a child remains Published.

## 10. Curriculum import

| Route | Result |
|---|---|
| `POST /api/admin/curriculum-imports/validate` | `200 CurriculumImportValidationDto` |
| `POST /api/admin/curriculum-imports` | `201 CurriculumImportSummaryDto`; `409` if validation fails |

Both require Administrator, use the 30/user/IP/minute mutation limiter, and cap the request at 5 MiB. Only case-insensitive `CreateOnly` mode is supported. Validation is read-only. Import revalidates, then creates everything inside one EF transaction: all or nothing. It creates Draft content; it does not publish. It never fetches URLs and request logging records only method/path/status/timing/user, not bodies.

Full request schema:

```ts
interface CurriculumImportRequest {
  category: { name: string; slug: string; description?: string | null };
  career: { title: string; slug: string; shortDescription: string; estimatedDurationWeeks?: number | null };
  skills: Array<{ name: string; slug: string; category: SkillCategory; description?: string | null }>;
  providers: Array<{ name: string; slug: string; websiteUrl?: string | null; description?: string | null }>;
  instructors?: Array<{ providerSlug: string; name: string; title?: string | null; profileUrl?: string | null }> | null;
  courses: Array<{
    title: string; slug: string; difficulty: CourseDifficulty; estimatedDurationMinutes: number;
    shortDescription?: string | null;
    skills: Array<{ skillSlug: string; proficiencyLevel: SkillProficiencyLevel; isPrimary: boolean }>;
    lessons: Array<{ title: string; slug: string; contentType: LessonContentType; estimatedDurationMinutes: number; order: number; isRequired: boolean; summary?: string | null; externalResourceUrl?: string | null }>;
    externalResources: Array<{ providerSlug: string; title: string; resourceType: ExternalResourceType; accessType: ResourceAccessType; url: string; order: number; isRequired: boolean; instructorName?: string | null; estimatedDurationMinutes?: number | null }>;
  }>;
  pathway: { name: string; version: string; isPrimary: boolean; description?: string | null; levels: Array<{ name: string; order: number; description?: string | null; courses: Array<{ courseSlug: string; order: number; isRequired: boolean }> }> };
  mode?: "CreateOnly";
}
```

Validation fields: `isValid`, `errors`, `warnings`, `counts`. Each issue has `path`, `code`, `message`. Counts are `categories`, `careers`, `skills`, `providers`, `instructors`, `resources`, `courses`, `lessons`, `pathways`, `levels`, `assignments`. Import result is `importId`, `created` counts, `categoryId`, `categorySlug`, `careerId`, `careerSlug`, `pathwayId`.

Limits: 100 skills, 100 providers, 200 instructors, 200 courses, 2,000 total lessons, 3,000 total external resources, 20 pathway levels, 500 pathway course assignments. It rejects duplicate in-document skill/provider/course slugs, duplicate order values, undeclared references, existing CreateOnly slugs, unsupported mode, and invalid external HTTP(S) URLs. Every external resource adds a `ReviewRequired` warning. Note that the model supports category, one career/pathway, skills, providers/instructors, courses with skills/lessons/external resources, and levels/assignments; it does **not** import native assessments, questions, answer options, projects, career-skill links, or course prerequisites.

## 11. Health and operations

| Route | Access | Meaning |
|---|---|---|
| `GET /health` | Anonymous | Liveness/self only; normally `200 Healthy`, independent of PostgreSQL. |
| `GET /health/ready` | Anonymous | `200` only when DB context exists, PostgreSQL connects, and there are no pending migrations; otherwise `503`. |
| `GET /swagger/index.html` | Anonymous in Development | Swagger UI; absent outside Development. |

Startup may apply migrations when `Database__ApplyMigrationsOnStartup=true`; readiness still fails when migrations remain pending. Docker endpoints are API `58080`, PostgreSQL `55433`.

## 12. Frontend workflow maps

### Registration and session restoration

1. Register → receive token pair and `Learner` user → store refresh token using the application's chosen secure persistence strategy and access token in memory where practical.
2. Call protected route with bearer access token.
3. On browser restoration, load saved refresh token → `POST /api/auth/refresh` → atomically store rotated pair → `GET /api/users/me`.
4. One coordinated refresh handles concurrent 401s; retry originals once; failed refresh clears session.

### Anonymous career discovery

1. Load categories → debounced paged career search → open career by slug.
2. Use embedded `primaryPathway` or load `/api/careers/{careerId}/pathway` → open course by slug.
3. Load lesson bodies on demand; separately load external resources, assessments, and projects.

### Learner enrollment and progress

1. Open Published career → enroll by career UUID → load enrollment detail.
2. Find first `Available` course → start → start/complete required lessons.
3. Open external URLs and self-confirm required resources; run/pass each native Published assessment.
4. Automatic evaluation completes the course; completing all required courses unlocks later levels and eventually completes enrollment. Use explicit `/complete` to show unmet requirement conflicts if desired.

### Native assessment

1. List summaries → start (or resume returned active attempt) → render questions/options without correctness.
2. Debounced autosave `PUT responses` serially; do not overlap writes blindly.
3. Submit all responses → show score/pass and per-response correctness → refresh course/enrollment detail.

### Administrator authoring

1. Create provider → publish provider → create optional instructor and publish → create external resource → mark reviewed and publish.
2. Create Draft skills and publish → create Draft course → add lessons/skills/prerequisites/resource assignments → publish course.
3. Create category/career, publish category/career → build Draft pathway levels/course assignments → publish pathway.

### Curriculum import

1. Build document → validate → display path-addressed errors/warnings/counts.
2. On valid confirmation, submit the identical document → receive created counts/IDs → refresh admin catalogs.
3. Imported content is Draft; continue review/publish workflow.

## 13. Frontend implementation warnings

- Do not infer DTOs from Domain entities or controller classes; use this guide/OpenAPI/application DTOs.
- Enums are case-sensitive strings; UUIDs are strings; all `*Utc` values are UTC instants.
- Use slug routes for public career/course/lesson/provider/skill lookups and UUID routes where declared.
- Do not automatically retry non-idempotent mutations. After refresh, retry an original request at most once.
- Never refresh on 403. Refresh tokens rotate; concurrent 401s must share one refresh call.
- Wrong ownership and hidden Draft/Archived public resources commonly return 404.
- Published content is generally immutable; archive is not an edit mode.
- Exception/risk: career category, career, and career-skill update paths do not currently enforce that immutability consistently.
- External resources open outside Careersity; external completion is self-confirmed.
- Never cache/expose native answer keys. Correct option IDs are never part of learner/public contracts.
- Hiding admin navigation is not authorization; the server still enforces Administrator.
- Map Problem Details validation keys case-insensitively; preserve correlation/trace IDs for support.
- Pagination/search/filtering are server-side; debounce search and cancel stale reads.
- Swagger currently applies Bearer globally and omits concrete response schemas/status metadata because controllers return `IActionResult`; do not generate a client from it without augmenting security/response metadata.
- The public provider/resource collection routes are arrays, not paged, despite admin equivalents being paged.
- Profile is `/api/users/me`, while learner records are `/api/me/**`.

## 14. TypeScript contract appendix

These are documentation examples reflecting runtime JSON. UUID and UTC aliases remain strings; validate untrusted JSON at the network boundary.

```ts
type UUID = string;
type UtcDateTime = string;
type ContentStatus = "Draft" | "Published" | "Archived";
type CourseDifficulty = "Foundation" | "Beginner" | "Intermediate" | "Advanced";
type LessonContentType = "Article" | "Video" | "Exercise" | "ExternalResource" | "Mixed";
type QuestionType = "SingleChoice" | "MultipleChoice" | "TrueFalse";
type SkillCategory = "Technical" | "Analytical" | "Communication" | "Professional" | "DomainKnowledge" | "Tool";
type SkillProficiencyLevel = "Awareness" | "Beginner" | "Intermediate" | "Advanced";
type ProjectSubmissionType = "RepositoryUrl" | "PortfolioUrl" | "DocumentUrl" | "VideoUrl" | "TextResponse" | "Mixed";
type UserRole = "Learner" | "Administrator";
type EnrollmentStatus = "Active" | "Paused" | "Completed" | "Withdrawn";
type AssessmentAttemptStatus = "InProgress" | "Passed" | "Failed";
type CourseAvailabilityStatus = "Locked" | "Available" | "InProgress" | "Completed";
type ExternalResourceType = "LectureVideo" | "VideoPlaylist" | "Reading" | "Exercise" | "ExternalAssessment" | "Assignment" | "AnswerGuide" | "CourseWebsite" | "CertificateOpportunity" | "Dataset" | "SoftwareTool" | "Other";
type ResourceAccessType = "Free" | "FreeWithAccount" | "AuditFree" | "Paid" | "InstitutionRestricted" | "Unknown";
interface PagedResult<T> { items: T[]; page: number; pageSize: number; totalCount: number }

interface CareerCategoryDto { id: UUID; name: string; slug: string; description: string | null; status: ContentStatus; createdAtUtc: UtcDateTime; updatedAtUtc: UtcDateTime | null }
interface CareerListItemDto { id: UUID; careerCategoryId: UUID; categoryName: string; title: string; slug: string; shortDescription: string; estimatedDurationWeeks: number | null; status: ContentStatus }
interface CareerSkillDto { id: UUID; skillId: UUID; skillName: string; skillSlug: string; skillCategory: SkillCategory; requiredProficiencyLevel: SkillProficiencyLevel; isRequired: boolean; displayOrder: number }
interface PathwayLevelCourseDto { id: UUID; courseId: UUID; courseTitle: string; courseSlug: string; difficulty: CourseDifficulty; estimatedDurationMinutes: number; order: number; isRequired: boolean }
interface PathwayLevelDto { id: UUID; name: string; description: string | null; order: number; courses: PathwayLevelCourseDto[] }
interface CareerPathwayDto { id: UUID; careerId: UUID; name: string; description: string | null; version: string; isPrimary: boolean; status: ContentStatus; levels: PathwayLevelDto[] }
interface CareerDetailDto { id: UUID; careerCategory: CareerCategoryDto; title: string; slug: string; shortDescription: string; detailedDescription: string | null; responsibilities: string | null; estimatedDurationWeeks: number | null; status: ContentStatus; skills: CareerSkillDto[]; primaryPathway: CareerPathwayDto | null; createdAtUtc: UtcDateTime; updatedAtUtc: UtcDateTime | null }
interface SkillListItemDto { id: UUID; name: string; slug: string; category: SkillCategory; status: ContentStatus }
interface SkillDto extends SkillListItemDto { description: string | null; createdAtUtc: UtcDateTime; updatedAtUtc: UtcDateTime | null }
interface LessonSummaryDto { id: UUID; title: string; slug: string; summary: string | null; contentType: LessonContentType; estimatedDurationMinutes: number; order: number; isRequired: boolean }
interface LessonDto extends LessonSummaryDto { courseId: UUID; content: string | null; externalResourceUrl: string | null; createdAtUtc: UtcDateTime; updatedAtUtc: UtcDateTime | null }
interface CoursePrerequisiteDto { id: UUID; prerequisiteCourseId: UUID; prerequisiteCourseTitle: string; prerequisiteCourseSlug: string; isRequired: boolean }
interface CourseSkillDto { id: UUID; skillId: UUID; skillName: string; skillSlug: string; skillCategory: SkillCategory; proficiencyLevel: SkillProficiencyLevel; isPrimary: boolean }
interface CourseListItemDto { id: UUID; title: string; slug: string; shortDescription: string; difficulty: CourseDifficulty; estimatedDurationMinutes: number; status: ContentStatus; lessonCount: number; skillCount: number }
interface CourseDetailDto { id: UUID; title: string; slug: string; shortDescription: string; detailedDescription: string | null; difficulty: CourseDifficulty; estimatedDurationMinutes: number; status: ContentStatus; lessons: LessonSummaryDto[]; prerequisites: CoursePrerequisiteDto[]; skills: CourseSkillDto[]; createdAtUtc: UtcDateTime; updatedAtUtc: UtcDateTime | null }
interface PublicAssessmentSummaryDto { id: UUID; title: string; description: string | null; passingScorePercentage: number; maximumAttempts: number | null; questionCount: number; totalPoints: number }
interface PublicProjectSummaryDto { id: UUID; title: string; description: string; submissionType: ProjectSubmissionType; estimatedDurationMinutes: number }
interface PublicProjectDetailDto extends PublicProjectSummaryDto { courseId: UUID; courseTitle: string; courseSlug: string; instructions: string; expectedOutput: string | null; evaluationCriteria: string | null }
interface LearningProviderDto { id: UUID; name: string; slug: string; description: string | null; websiteUrl: string | null; logoUrl: string | null; status: ContentStatus; createdAtUtc: UtcDateTime; updatedAtUtc: UtcDateTime | null }
interface InstructorDto { id: UUID; learningProviderId: UUID; providerName: string; name: string; title: string | null; biography: string | null; profileUrl: string | null; status: ContentStatus; createdAtUtc: UtcDateTime; updatedAtUtc: UtcDateTime | null }
interface ExternalLearningResourceListItemDto { id: UUID; title: string; resourceType: ExternalResourceType; accessType: ResourceAccessType; url: string; providerId: UUID; providerName: string; instructorId: UUID | null; instructorName: string | null; estimatedDurationMinutes: number | null; lastReviewedAtUtc: UtcDateTime | null; status: ContentStatus }
interface ExternalLearningResourceDetailDto { id: UUID; title: string; description: string | null; resourceType: ExternalResourceType; accessType: ResourceAccessType; url: string; sourceLabel: string | null; provider: LearningProviderDto; instructor: InstructorDto | null; estimatedDurationMinutes: number | null; lastReviewedAtUtc: UtcDateTime | null; status: ContentStatus; createdAtUtc: UtcDateTime; updatedAtUtc: UtcDateTime | null }
interface PublicExternalResourceDto { resourceId: UUID; title: string; description: string | null; resourceType: ExternalResourceType; accessType: ResourceAccessType; url: string; sourceLabel: string | null; providerName: string; instructorName: string | null; estimatedDurationMinutes: number | null; order: number; isRequired: boolean }

interface EnrollmentCareerDto { id: UUID; title: string; slug: string }
interface EnrollmentPathwayDto { id: UUID; name: string; version: string }
interface CurrentCourseDto { courseId: UUID; title: string; slug: string; pathwayLevelName: string; pathwayLevelOrder: number; courseOrder: number; progressPercentage: number }
interface LearnerLessonProgressDto { lessonId: UUID; title: string; slug: string; summary: string | null; contentType: LessonContentType; estimatedDurationMinutes: number; order: number; isRequired: boolean; isStarted: boolean; isCompleted: boolean; startedAtUtc: UtcDateTime | null; completedAtUtc: UtcDateTime | null }
interface LearnerCourseProgressDto { courseId: UUID; title: string; slug: string; difficulty: CourseDifficulty; estimatedDurationMinutes: number; order: number; isRequired: boolean; availabilityStatus: CourseAvailabilityStatus; isStarted: boolean; isCompleted: boolean; progressPercentage: number; completedLessonCount: number; totalRequiredLessonCount: number; startedAtUtc: UtcDateTime | null; completedAtUtc: UtcDateTime | null; completedRequiredExternalResourceCount: number; totalRequiredExternalResourceCount: number }
interface LearnerPathwayLevelDto { id: UUID; name: string; description: string | null; order: number; isCompleted: boolean; progressPercentage: number; courses: LearnerCourseProgressDto[] }
interface CareerEnrollmentListItemDto { id: UUID; careerId: UUID; careerTitle: string; careerSlug: string; careerPathwayId: UUID; pathwayName: string; pathwayVersion: string; status: EnrollmentStatus; enrolledAtUtc: UtcDateTime; startedAtUtc: UtcDateTime | null; completedAtUtc: UtcDateTime | null; overallProgressPercentage: number; completedCourseCount: number; totalRequiredCourseCount: number; currentCourse: CurrentCourseDto | null }
interface CareerEnrollmentDetailDto { id: UUID; career: EnrollmentCareerDto; pathway: EnrollmentPathwayDto; status: EnrollmentStatus; enrolledAtUtc: UtcDateTime; startedAtUtc: UtcDateTime | null; pausedAtUtc: UtcDateTime | null; completedAtUtc: UtcDateTime | null; withdrawnAtUtc: UtcDateTime | null; overallProgressPercentage: number; completedRequiredCourseCount: number; totalRequiredCourseCount: number; levels: LearnerPathwayLevelDto[] }
interface LearnerExternalResourceProgressDto extends Omit<PublicExternalResourceDto, "resourceId" | "sourceLabel"> { assignmentId: UUID; resourceId: UUID; isStarted: boolean; isCompleted: boolean; startedAtUtc: UtcDateTime | null; completedAtUtc: UtcDateTime | null }
interface LearnerCourseDetailDto { courseId: UUID; title: string; slug: string; shortDescription: string; detailedDescription: string | null; difficulty: CourseDifficulty; estimatedDurationMinutes: number; availabilityStatus: CourseAvailabilityStatus; isStarted: boolean; isCompleted: boolean; progressPercentage: number; prerequisites: CoursePrerequisiteDto[]; lessons: LearnerLessonProgressDto[]; projects: PublicProjectSummaryDto[]; assessmentSummaries: PublicAssessmentSummaryDto[]; externalResources: LearnerExternalResourceProgressDto[] | null; completedRequiredExternalResourceCount: number; totalRequiredExternalResourceCount: number }
interface LearnerLessonDetailDto extends LearnerLessonProgressDto { courseId: UUID; content: string | null; externalResourceUrl: string | null }
interface LearnerAssessmentSummaryDto { assessmentId: UUID; title: string; description: string | null; passingScorePercentage: number; maximumAttempts: number | null; attemptsUsed: number; attemptsRemaining: number | null; hasPassed: boolean; activeAttemptId: UUID | null; questionCount: number; totalPoints: number }
interface LearnerAnswerOptionDto { answerOptionId: UUID; text: string; order: number }
interface LearnerAssessmentQuestionDto { questionId: UUID; prompt: string; questionType: QuestionType; order: number; points: number; answerOptions: LearnerAnswerOptionDto[] }
interface AssessmentAttemptStartDto { attemptId: UUID; assessmentId: UUID; attemptNumber: number; status: AssessmentAttemptStatus; startedAtUtc: UtcDateTime; questions: LearnerAssessmentQuestionDto[] }
interface LearnerAssessmentResponseDto { questionId: UUID; selectedAnswerOptionIds: UUID[]; isCorrect: boolean | null; pointsAwarded: number | null }
interface AssessmentAttemptDetailDto { attemptId: UUID; assessmentId: UUID; assessmentTitle: string; attemptNumber: number; status: AssessmentAttemptStatus; startedAtUtc: UtcDateTime; submittedAtUtc: UtcDateTime | null; scorePercentage: number | null; pointsEarned: number | null; totalPoints: number | null; passed: boolean; responses: LearnerAssessmentResponseDto[] }
interface AssessmentAttemptHistoryItemDto { attemptId: UUID; attemptNumber: number; status: AssessmentAttemptStatus; startedAtUtc: UtcDateTime; submittedAtUtc: UtcDateTime | null; scorePercentage: number | null; passed: boolean }
interface SaveAssessmentResponseRequest { questionId: UUID; selectedAnswerOptionIds: UUID[] }
interface SaveAssessmentResponsesRequest { responses: SaveAssessmentResponseRequest[] }
interface SubmitAssessmentAttemptRequest { responses?: SaveAssessmentResponseRequest[] | null }
```

### Administrator contract examples

All admin request fields are exact; shared response DTOs are above. Assessment/project admin response-only contracts follow.

```ts
interface CreateCareerCategoryRequest { name: string; slug: string; description: string | null }
type UpdateCareerCategoryRequest = CreateCareerCategoryRequest;
interface CreateCareerRequest { careerCategoryId: UUID; title: string; slug: string; shortDescription: string; detailedDescription: string | null; responsibilities: string | null; estimatedDurationWeeks: number | null }
type UpdateCareerRequest = Omit<CreateCareerRequest, "careerCategoryId">;
interface ChangeCareerCategoryRequest { careerCategoryId: UUID }
interface AssignCareerSkillRequest { skillId: UUID; requiredProficiencyLevel: SkillProficiencyLevel; isRequired: boolean; displayOrder: number }
type UpdateCareerSkillRequest = Omit<AssignCareerSkillRequest, "skillId">;
interface CreateCareerPathwayRequest { careerId: UUID; name: string; description: string | null; version: string; isPrimary: boolean }
type UpdateCareerPathwayRequest = Omit<CreateCareerPathwayRequest, "careerId">;
interface AddPathwayLevelRequest { name: string; description: string | null; order: number }
type UpdatePathwayLevelRequest = AddPathwayLevelRequest;
interface AddPathwayLevelCourseRequest { courseId: UUID; order: number; isRequired: boolean }
interface UpdatePathwayLevelCourseRequest { order: number; isRequired: boolean }
interface ReorderPathwayLevelsRequest { levels: Array<{ levelId: UUID; order: number }> }
interface ReorderPathwayCoursesRequest { courses: Array<{ assignmentId: UUID; order: number }> }
interface CreateSkillRequest { name: string; slug: string; description: string | null; category: SkillCategory }
type UpdateSkillRequest = CreateSkillRequest;
interface CreateCourseRequest { title: string; slug: string; shortDescription: string; detailedDescription: string | null; difficulty: CourseDifficulty; estimatedDurationMinutes: number }
type UpdateCourseRequest = CreateCourseRequest;
interface AddLessonRequest { title: string; slug: string; summary: string | null; content: string | null; contentType: LessonContentType; externalResourceUrl: string | null; estimatedDurationMinutes: number; order: number; isRequired: boolean }
type UpdateLessonRequest = AddLessonRequest;
interface ReorderLessonsRequest { lessons: Array<{ lessonId: UUID; order: number }> }
interface AddCoursePrerequisiteRequest { prerequisiteCourseId: UUID; isRequired: boolean }
interface UpdateCoursePrerequisiteRequest { isRequired: boolean }
interface AddCourseSkillRequest { skillId: UUID; proficiencyLevel: SkillProficiencyLevel; isPrimary: boolean }
interface UpdateCourseSkillRequest { proficiencyLevel: SkillProficiencyLevel; isPrimary: boolean }
interface CreateAssessmentRequest { courseId: UUID; title: string; description: string | null; passingScorePercentage: number; maximumAttempts: number | null }
type UpdateAssessmentRequest = Omit<CreateAssessmentRequest, "courseId">;
interface AddQuestionRequest { prompt: string; questionType: QuestionType; order: number; points: number }
type UpdateQuestionRequest = AddQuestionRequest;
interface ReorderQuestionsRequest { questions: Array<{ questionId: UUID; order: number }> }
interface AddAnswerOptionRequest { text: string; isCorrect: boolean; order: number }
type UpdateAnswerOptionRequest = AddAnswerOptionRequest;
interface ReorderAnswerOptionsRequest { answerOptions: Array<{ answerOptionId: UUID; order: number }> }
interface CreateProjectRequest { courseId: UUID; title: string; description: string; instructions: string; expectedOutput: string | null; evaluationCriteria: string | null; submissionType: ProjectSubmissionType; estimatedDurationMinutes: number }
type UpdateProjectRequest = Omit<CreateProjectRequest, "courseId">;
interface CreateLearningProviderRequest { name: string; slug: string; description: string | null; websiteUrl: string | null; logoUrl: string | null }
type UpdateLearningProviderRequest = CreateLearningProviderRequest;
interface CreateInstructorRequest { learningProviderId: UUID; name: string; title: string | null; biography: string | null; profileUrl: string | null }
type UpdateInstructorRequest = Omit<CreateInstructorRequest, "learningProviderId">;
interface ChangeInstructorProviderRequest { learningProviderId: UUID }
interface CreateExternalLearningResourceRequest { learningProviderId: UUID; instructorId: UUID | null; title: string; description: string | null; resourceType: ExternalResourceType; accessType: ResourceAccessType; url: string; sourceLabel: string | null; estimatedDurationMinutes: number | null }
type UpdateExternalLearningResourceRequest = CreateExternalLearningResourceRequest;
interface AssignExternalResourceToCourseRequest { externalLearningResourceId: UUID; order: number; isRequired: boolean; notes: string | null }
interface UpdateCourseExternalResourceRequest { order: number; isRequired: boolean; notes: string | null }
interface ReorderCourseExternalResourcesRequest { resources: Array<{ assignmentId: UUID; order: number }> }
interface AnswerOptionAdminDto { id: UUID; questionId: UUID; text: string; isCorrect: boolean; order: number; createdAtUtc: UtcDateTime; updatedAtUtc: UtcDateTime | null }
interface QuestionAdminDto { id: UUID; assessmentId: UUID; prompt: string; questionType: QuestionType; order: number; points: number; answerOptions: AnswerOptionAdminDto[]; createdAtUtc: UtcDateTime; updatedAtUtc: UtcDateTime | null }
interface AssessmentListItemDto { id: UUID; courseId: UUID; title: string; description: string | null; passingScorePercentage: number; maximumAttempts: number | null; status: ContentStatus; questionCount: number; totalPoints: number }
interface AssessmentAdminDetailDto { id: UUID; courseId: UUID; courseTitle: string; title: string; description: string | null; passingScorePercentage: number; maximumAttempts: number | null; status: ContentStatus; questions: QuestionAdminDto[]; createdAtUtc: UtcDateTime; updatedAtUtc: UtcDateTime | null }
interface ProjectListItemDto { id: UUID; courseId: UUID; courseTitle: string; title: string; description: string; submissionType: ProjectSubmissionType; estimatedDurationMinutes: number; status: ContentStatus }
interface ProjectAdminDetailDto extends ProjectListItemDto { instructions: string; expectedOutput: string | null; evaluationCriteria: string | null; createdAtUtc: UtcDateTime; updatedAtUtc: UtcDateTime | null }
interface CourseExternalResourceDto { assignmentId: UUID; courseId: UUID; resourceId: UUID; title: string; description: string | null; resourceType: ExternalResourceType; accessType: ResourceAccessType; url: string; providerName: string; instructorName: string | null; order: number; isRequired: boolean; notes: string | null; estimatedDurationMinutes: number | null }
interface CurriculumImportIssue { path: string; code: string; message: string }
interface CurriculumImportCounts { categories: number; careers: number; skills: number; providers: number; instructors: number; resources: number; courses: number; lessons: number; pathways: number; levels: number; assignments: number }
interface CurriculumImportValidationDto { isValid: boolean; errors: CurriculumImportIssue[]; warnings: CurriculumImportIssue[]; counts: CurriculumImportCounts }
interface CurriculumImportSummaryDto { importId: UUID; created: CurriculumImportCounts; categoryId: UUID; categorySlug: string; careerId: UUID; careerSlug: string; pathwayId: UUID }
```

## 15. OpenAPI status and source reconciliation

The checked-in `docs/careersity-openapi.json` is valid OpenAPI 3.0.1 exported from Development `/swagger/v1/swagger.json`. It contains all 149 controller operations, the Bearer scheme, and 85 request/enum schemas. No secret values occur in it.

Known Swagger limitations: all operations inherit a global Bearer requirement even though public/auth/health routes are anonymous; protected/public operations are therefore **not distinguishable from the JSON alone**. Controller actions return `IActionResult` without `ProducesResponseType`, so generated operations generally advertise only `200` and omit response schemas even when actual status is `201`/`204`. Health endpoints are mapped endpoints and do not appear in the controller-generated document. Use this guide plus controller source for access/status/response truth until Swagger metadata is improved.

README/source discrepancies found: README local database examples still use `localhost:5432` with `postgres` credentials, while Compose and the current Development configuration use host port `55433` and the configured Careersity role. README correctly lists launch URLs, but it cannot express the Swagger response/security limitations above. The requested conceptual name `UserProfile` maps to the actual `/api/users/me`, not `/api/me/profile`. Public learning-provider and external-resource lists are unpaged arrays even though the corresponding admin lists use pagination.

Test-isolation risk: the two database-free `HealthEndpointTests` inherit tracked Development configuration. With the local Development connection/migration settings present, the unqualified suite does not exercise its intended unavailable-database branch. The suite passes when that test process explicitly supplies a blank connection and `Database__ApplyMigrationsOnStartup=false`; production code/tests were not changed here.

Source precedence used: controller route/action → application request/DTO → validator → service/domain behavior → integration/unit tests → README. No endpoint described here is absent from the 149-operation controller/OpenAPI inventory; operational health/Swagger endpoints are the additional mapped/middleware endpoints.

## Appendix A. Exhaustive controller route index

This mechanically reconciled index contains every controller operation in the exported v1 document. `Body` is the OpenAPI request schema. Access is corrected from controller attributes because the current OpenAPI security requirement is global.

| Method | Exact path | Access | Body |
|---|---|---|---|
| GET | `/api/admin/assessments` | Administrator | `—` |
| POST | `/api/admin/assessments` | Administrator | `CreateAssessmentRequest` |
| GET | `/api/admin/assessments/{assessmentId}` | Administrator | `—` |
| PUT | `/api/admin/assessments/{assessmentId}` | Administrator | `UpdateAssessmentRequest` |
| POST | `/api/admin/assessments/{assessmentId}/archive` | Administrator | `—` |
| POST | `/api/admin/assessments/{assessmentId}/publish` | Administrator | `—` |
| POST | `/api/admin/assessments/{assessmentId}/questions` | Administrator | `AddQuestionRequest` |
| GET | `/api/admin/assessments/{assessmentId}/questions/{questionId}` | Administrator | `—` |
| PUT | `/api/admin/assessments/{assessmentId}/questions/{questionId}` | Administrator | `UpdateQuestionRequest` |
| DELETE | `/api/admin/assessments/{assessmentId}/questions/{questionId}` | Administrator | `—` |
| POST | `/api/admin/assessments/{assessmentId}/questions/{questionId}/answer-options` | Administrator | `AddAnswerOptionRequest` |
| PUT | `/api/admin/assessments/{assessmentId}/questions/{questionId}/answer-options/{answerOptionId}` | Administrator | `UpdateAnswerOptionRequest` |
| DELETE | `/api/admin/assessments/{assessmentId}/questions/{questionId}/answer-options/{answerOptionId}` | Administrator | `—` |
| PUT | `/api/admin/assessments/{assessmentId}/questions/{questionId}/answer-options/reorder` | Administrator | `ReorderAnswerOptionsRequest` |
| PUT | `/api/admin/assessments/{assessmentId}/questions/reorder` | Administrator | `ReorderQuestionsRequest` |
| GET | `/api/admin/career-categories` | Administrator | `—` |
| POST | `/api/admin/career-categories` | Administrator | `CreateCareerCategoryRequest` |
| GET | `/api/admin/career-categories/{id}` | Administrator | `—` |
| PUT | `/api/admin/career-categories/{id}` | Administrator | `UpdateCareerCategoryRequest` |
| POST | `/api/admin/career-categories/{id}/archive` | Administrator | `—` |
| POST | `/api/admin/career-categories/{id}/publish` | Administrator | `—` |
| GET | `/api/admin/careers` | Administrator | `—` |
| POST | `/api/admin/careers` | Administrator | `CreateCareerRequest` |
| GET | `/api/admin/careers/{careerId}/pathways` | Administrator | `—` |
| POST | `/api/admin/careers/{careerId}/pathways` | Administrator | `CreateCareerPathwayRequest` |
| GET | `/api/admin/careers/{careerId}/pathways/{pathwayId}` | Administrator | `—` |
| PUT | `/api/admin/careers/{careerId}/pathways/{pathwayId}` | Administrator | `UpdateCareerPathwayRequest` |
| POST | `/api/admin/careers/{careerId}/pathways/{pathwayId}/archive` | Administrator | `—` |
| POST | `/api/admin/careers/{careerId}/pathways/{pathwayId}/levels` | Administrator | `AddPathwayLevelRequest` |
| PUT | `/api/admin/careers/{careerId}/pathways/{pathwayId}/levels/{levelId}` | Administrator | `UpdatePathwayLevelRequest` |
| DELETE | `/api/admin/careers/{careerId}/pathways/{pathwayId}/levels/{levelId}` | Administrator | `—` |
| POST | `/api/admin/careers/{careerId}/pathways/{pathwayId}/levels/{levelId}/courses` | Administrator | `AddPathwayLevelCourseRequest` |
| PUT | `/api/admin/careers/{careerId}/pathways/{pathwayId}/levels/{levelId}/courses/{assignmentId}` | Administrator | `UpdatePathwayLevelCourseRequest` |
| DELETE | `/api/admin/careers/{careerId}/pathways/{pathwayId}/levels/{levelId}/courses/{assignmentId}` | Administrator | `—` |
| PUT | `/api/admin/careers/{careerId}/pathways/{pathwayId}/levels/{levelId}/courses/reorder` | Administrator | `ReorderPathwayCoursesRequest` |
| PUT | `/api/admin/careers/{careerId}/pathways/{pathwayId}/levels/reorder` | Administrator | `ReorderPathwayLevelsRequest` |
| POST | `/api/admin/careers/{careerId}/pathways/{pathwayId}/publish` | Administrator | `—` |
| GET | `/api/admin/careers/{careerId}/skills` | Administrator | `—` |
| POST | `/api/admin/careers/{careerId}/skills` | Administrator | `AssignCareerSkillRequest` |
| PUT | `/api/admin/careers/{careerId}/skills/{careerSkillId}` | Administrator | `UpdateCareerSkillRequest` |
| DELETE | `/api/admin/careers/{careerId}/skills/{careerSkillId}` | Administrator | `—` |
| GET | `/api/admin/careers/{id}` | Administrator | `—` |
| PUT | `/api/admin/careers/{id}` | Administrator | `UpdateCareerRequest` |
| POST | `/api/admin/careers/{id}/archive` | Administrator | `—` |
| PUT | `/api/admin/careers/{id}/category` | Administrator | `ChangeCareerCategoryRequest` |
| POST | `/api/admin/careers/{id}/publish` | Administrator | `—` |
| GET | `/api/admin/courses` | Administrator | `—` |
| POST | `/api/admin/courses` | Administrator | `CreateCourseRequest` |
| GET | `/api/admin/courses/{courseId}/external-resources` | Administrator | `—` |
| POST | `/api/admin/courses/{courseId}/external-resources` | Administrator | `AssignExternalResourceToCourseRequest` |
| PUT | `/api/admin/courses/{courseId}/external-resources/{assignmentId}` | Administrator | `UpdateCourseExternalResourceRequest` |
| DELETE | `/api/admin/courses/{courseId}/external-resources/{assignmentId}` | Administrator | `—` |
| PUT | `/api/admin/courses/{courseId}/external-resources/reorder` | Administrator | `ReorderCourseExternalResourcesRequest` |
| POST | `/api/admin/courses/{courseId}/lessons` | Administrator | `AddLessonRequest` |
| GET | `/api/admin/courses/{courseId}/lessons/{lessonId}` | Administrator | `—` |
| PUT | `/api/admin/courses/{courseId}/lessons/{lessonId}` | Administrator | `UpdateLessonRequest` |
| DELETE | `/api/admin/courses/{courseId}/lessons/{lessonId}` | Administrator | `—` |
| PUT | `/api/admin/courses/{courseId}/lessons/reorder` | Administrator | `ReorderLessonsRequest` |
| POST | `/api/admin/courses/{courseId}/prerequisites` | Administrator | `AddCoursePrerequisiteRequest` |
| PUT | `/api/admin/courses/{courseId}/prerequisites/{prerequisiteId}` | Administrator | `UpdateCoursePrerequisiteRequest` |
| DELETE | `/api/admin/courses/{courseId}/prerequisites/{prerequisiteId}` | Administrator | `—` |
| POST | `/api/admin/courses/{courseId}/skills` | Administrator | `AddCourseSkillRequest` |
| PUT | `/api/admin/courses/{courseId}/skills/{courseSkillId}` | Administrator | `UpdateCourseSkillRequest` |
| DELETE | `/api/admin/courses/{courseId}/skills/{courseSkillId}` | Administrator | `—` |
| GET | `/api/admin/courses/{id}` | Administrator | `—` |
| PUT | `/api/admin/courses/{id}` | Administrator | `UpdateCourseRequest` |
| POST | `/api/admin/courses/{id}/archive` | Administrator | `—` |
| POST | `/api/admin/courses/{id}/publish` | Administrator | `—` |
| POST | `/api/admin/curriculum-imports` | Administrator | `CurriculumImportRequest` |
| POST | `/api/admin/curriculum-imports/validate` | Administrator | `CurriculumImportRequest` |
| GET | `/api/admin/external-learning-resources` | Administrator | `—` |
| POST | `/api/admin/external-learning-resources` | Administrator | `CreateExternalLearningResourceRequest` |
| GET | `/api/admin/external-learning-resources/{id}` | Administrator | `—` |
| PUT | `/api/admin/external-learning-resources/{id}` | Administrator | `UpdateExternalLearningResourceRequest` |
| POST | `/api/admin/external-learning-resources/{id}/archive` | Administrator | `—` |
| POST | `/api/admin/external-learning-resources/{id}/mark-reviewed` | Administrator | `—` |
| POST | `/api/admin/external-learning-resources/{id}/publish` | Administrator | `—` |
| GET | `/api/admin/instructors` | Administrator | `—` |
| POST | `/api/admin/instructors` | Administrator | `CreateInstructorRequest` |
| GET | `/api/admin/instructors/{id}` | Administrator | `—` |
| PUT | `/api/admin/instructors/{id}` | Administrator | `UpdateInstructorRequest` |
| POST | `/api/admin/instructors/{id}/archive` | Administrator | `—` |
| PUT | `/api/admin/instructors/{id}/provider` | Administrator | `ChangeInstructorProviderRequest` |
| POST | `/api/admin/instructors/{id}/publish` | Administrator | `—` |
| GET | `/api/admin/learning-providers` | Administrator | `—` |
| POST | `/api/admin/learning-providers` | Administrator | `CreateLearningProviderRequest` |
| GET | `/api/admin/learning-providers/{id}` | Administrator | `—` |
| PUT | `/api/admin/learning-providers/{id}` | Administrator | `UpdateLearningProviderRequest` |
| POST | `/api/admin/learning-providers/{id}/archive` | Administrator | `—` |
| POST | `/api/admin/learning-providers/{id}/publish` | Administrator | `—` |
| GET | `/api/admin/projects` | Administrator | `—` |
| POST | `/api/admin/projects` | Administrator | `CreateProjectRequest` |
| GET | `/api/admin/projects/{projectId}` | Administrator | `—` |
| PUT | `/api/admin/projects/{projectId}` | Administrator | `UpdateProjectRequest` |
| POST | `/api/admin/projects/{projectId}/archive` | Administrator | `—` |
| POST | `/api/admin/projects/{projectId}/publish` | Administrator | `—` |
| GET | `/api/admin/skills` | Administrator | `—` |
| POST | `/api/admin/skills` | Administrator | `CreateSkillRequest` |
| GET | `/api/admin/skills/{id}` | Administrator | `—` |
| PUT | `/api/admin/skills/{id}` | Administrator | `UpdateSkillRequest` |
| POST | `/api/admin/skills/{id}/archive` | Administrator | `—` |
| POST | `/api/admin/skills/{id}/publish` | Administrator | `—` |
| POST | `/api/auth/change-password` | Authenticated | `ChangeMyPasswordRequest` |
| POST | `/api/auth/login` | Anonymous | `LoginRequest` |
| POST | `/api/auth/logout` | Authenticated | `LogoutRequest` |
| POST | `/api/auth/refresh` | Anonymous | `RefreshAccessTokenRequest` |
| POST | `/api/auth/register` | Anonymous | `RegisterRequest` |
| POST | `/api/auth/revoke-all` | Authenticated | `—` |
| GET | `/api/career-categories` | Anonymous | `—` |
| GET | `/api/careers` | Anonymous | `—` |
| GET | `/api/careers/{careerId}/pathway` | Anonymous | `—` |
| GET | `/api/careers/{slug}` | Anonymous | `—` |
| GET | `/api/courses` | Anonymous | `—` |
| GET | `/api/courses/{courseSlug}/assessments` | Anonymous | `—` |
| GET | `/api/courses/{courseSlug}/external-resources` | Anonymous | `—` |
| GET | `/api/courses/{courseSlug}/lessons/{lessonSlug}` | Anonymous | `—` |
| GET | `/api/courses/{courseSlug}/projects` | Anonymous | `—` |
| GET | `/api/courses/{courseSlug}/projects/{projectId}` | Anonymous | `—` |
| GET | `/api/courses/{slug}` | Anonymous | `—` |
| GET | `/api/external-learning-resources` | Anonymous | `—` |
| GET | `/api/external-learning-resources/{resourceId}` | Anonymous | `—` |
| GET | `/api/learning-providers` | Anonymous | `—` |
| GET | `/api/learning-providers/{providerId}/instructors` | Anonymous | `—` |
| GET | `/api/learning-providers/{slug}` | Anonymous | `—` |
| GET | `/api/me/career-enrollments` | Authenticated | `—` |
| POST | `/api/me/career-enrollments` | Authenticated | `EnrollInCareerRequest` |
| GET | `/api/me/career-enrollments/{enrollmentId}` | Authenticated | `—` |
| GET | `/api/me/career-enrollments/{enrollmentId}/courses/{courseId}` | Authenticated | `—` |
| GET | `/api/me/career-enrollments/{enrollmentId}/courses/{courseId}/assessments` | Authenticated | `—` |
| GET | `/api/me/career-enrollments/{enrollmentId}/courses/{courseId}/assessments/{assessmentId}/attempts` | Authenticated | `—` |
| POST | `/api/me/career-enrollments/{enrollmentId}/courses/{courseId}/assessments/{assessmentId}/attempts` | Authenticated | `—` |
| GET | `/api/me/career-enrollments/{enrollmentId}/courses/{courseId}/assessments/{assessmentId}/attempts/{attemptId}` | Authenticated | `—` |
| PUT | `/api/me/career-enrollments/{enrollmentId}/courses/{courseId}/assessments/{assessmentId}/attempts/{attemptId}/responses` | Authenticated | `SaveAssessmentResponsesRequest` |
| POST | `/api/me/career-enrollments/{enrollmentId}/courses/{courseId}/assessments/{assessmentId}/attempts/{attemptId}/submit` | Authenticated | `SubmitAssessmentAttemptRequest` |
| POST | `/api/me/career-enrollments/{enrollmentId}/courses/{courseId}/complete` | Authenticated | `—` |
| GET | `/api/me/career-enrollments/{enrollmentId}/courses/{courseId}/external-resources` | Authenticated | `—` |
| POST | `/api/me/career-enrollments/{enrollmentId}/courses/{courseId}/external-resources/{assignmentId}/complete` | Authenticated | `—` |
| POST | `/api/me/career-enrollments/{enrollmentId}/courses/{courseId}/external-resources/{assignmentId}/start` | Authenticated | `—` |
| GET | `/api/me/career-enrollments/{enrollmentId}/courses/{courseId}/lessons/{lessonId}` | Authenticated | `—` |
| POST | `/api/me/career-enrollments/{enrollmentId}/courses/{courseId}/lessons/{lessonId}/complete` | Authenticated | `—` |
| POST | `/api/me/career-enrollments/{enrollmentId}/courses/{courseId}/lessons/{lessonId}/start` | Authenticated | `—` |
| POST | `/api/me/career-enrollments/{enrollmentId}/courses/{courseId}/start` | Authenticated | `—` |
| POST | `/api/me/career-enrollments/{enrollmentId}/pause` | Authenticated | `—` |
| POST | `/api/me/career-enrollments/{enrollmentId}/resume` | Authenticated | `—` |
| POST | `/api/me/career-enrollments/{enrollmentId}/withdraw` | Authenticated | `—` |
| GET | `/api/skills` | Anonymous | `—` |
| GET | `/api/skills/{slug}` | Anonymous | `—` |
| GET | `/api/users/me` | Authenticated | `—` |
| PUT | `/api/users/me` | Authenticated | `UpdateMyProfileRequest` |
