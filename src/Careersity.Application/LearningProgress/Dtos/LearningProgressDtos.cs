using Careersity.Application.CurriculumActivities.Dtos;
using Careersity.Application.LearningContent.Dtos;
using Careersity.Domain.Enums;
using Careersity.Application.ExternalLearning.Dtos;

namespace Careersity.Application.LearningProgress.Dtos;

public enum CourseAvailabilityStatus { Locked, Available, InProgress, Completed }
public sealed record CurrentCourseDto(Guid CourseId, string Title, string Slug, string PathwayLevelName,
    int PathwayLevelOrder, int CourseOrder, decimal ProgressPercentage);
public sealed record CareerEnrollmentListItemDto(Guid Id, Guid CareerId, string CareerTitle, string CareerSlug,
    Guid CareerPathwayId, string PathwayName, string PathwayVersion, EnrollmentStatus Status,
    DateTimeOffset EnrolledAtUtc, DateTimeOffset? StartedAtUtc, DateTimeOffset? CompletedAtUtc,
    decimal OverallProgressPercentage, int CompletedCourseCount, int TotalRequiredCourseCount, CurrentCourseDto? CurrentCourse);
public sealed record LearnerLessonProgressDto(Guid LessonId, string Title, string Slug, string? Summary,
    LessonContentType ContentType, int EstimatedDurationMinutes, int Order, bool IsRequired, bool IsStarted,
    bool IsCompleted, DateTimeOffset? StartedAtUtc, DateTimeOffset? CompletedAtUtc);
public sealed record LearnerCourseProgressDto(Guid CourseId, string Title, string Slug, CourseDifficulty Difficulty,
    int EstimatedDurationMinutes, int Order, bool IsRequired, CourseAvailabilityStatus AvailabilityStatus,
    bool IsStarted, bool IsCompleted, decimal ProgressPercentage, int CompletedLessonCount,
    int TotalRequiredLessonCount, DateTimeOffset? StartedAtUtc, DateTimeOffset? CompletedAtUtc,
    int CompletedRequiredExternalResourceCount = 0, int TotalRequiredExternalResourceCount = 0);
public sealed record LearnerPathwayLevelDto(Guid Id, string Name, string? Description, int Order, bool IsCompleted,
    decimal ProgressPercentage, IReadOnlyCollection<LearnerCourseProgressDto> Courses);
public sealed record EnrollmentCareerDto(Guid Id, string Title, string Slug);
public sealed record EnrollmentPathwayDto(Guid Id, string Name, string Version);
public sealed record CareerEnrollmentDetailDto(Guid Id, EnrollmentCareerDto Career, EnrollmentPathwayDto Pathway,
    EnrollmentStatus Status, DateTimeOffset EnrolledAtUtc, DateTimeOffset? StartedAtUtc, DateTimeOffset? PausedAtUtc,
    DateTimeOffset? CompletedAtUtc, DateTimeOffset? WithdrawnAtUtc, decimal OverallProgressPercentage,
    int CompletedRequiredCourseCount, int TotalRequiredCourseCount, IReadOnlyCollection<LearnerPathwayLevelDto> Levels);
public sealed record LearnerCourseDetailDto(Guid CourseId, string Title, string Slug, string ShortDescription,
    string? DetailedDescription, CourseDifficulty Difficulty, int EstimatedDurationMinutes,
    CourseAvailabilityStatus AvailabilityStatus, bool IsStarted, bool IsCompleted, decimal ProgressPercentage,
    IReadOnlyCollection<CoursePrerequisiteDto> Prerequisites, IReadOnlyCollection<LearnerLessonProgressDto> Lessons,
    IReadOnlyCollection<PublicProjectSummaryDto> Projects, IReadOnlyCollection<PublicAssessmentSummaryDto> AssessmentSummaries,
    IReadOnlyCollection<LearnerExternalResourceProgressDto>? ExternalResources = null,
    int CompletedRequiredExternalResourceCount = 0, int TotalRequiredExternalResourceCount = 0);
public sealed record LearnerLessonDetailDto(Guid LessonId, Guid CourseId, string Title, string Slug, string? Summary,
    string? Content, LessonContentType ContentType, string? ExternalResourceUrl, int EstimatedDurationMinutes,
    int Order, bool IsRequired, bool IsStarted, bool IsCompleted, DateTimeOffset? StartedAtUtc, DateTimeOffset? CompletedAtUtc);
