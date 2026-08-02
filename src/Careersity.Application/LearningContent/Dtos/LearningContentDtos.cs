using Careersity.Domain.Enums;

namespace Careersity.Application.LearningContent.Dtos;

public sealed record SkillDto(Guid Id, string Name, string Slug, string? Description, SkillCategory Category,
    ContentStatus Status, DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);
public sealed record SkillListItemDto(Guid Id, string Name, string Slug, SkillCategory Category, ContentStatus Status);
public sealed record LessonSummaryDto(Guid Id, string Title, string Slug, string? Summary, LessonContentType ContentType,
    int EstimatedDurationMinutes, int Order, bool IsRequired);
public sealed record LessonDto(Guid Id, Guid CourseId, string Title, string Slug, string? Summary, string? Content,
    LessonContentType ContentType, string? ExternalResourceUrl, int EstimatedDurationMinutes, int Order,
    bool IsRequired, DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);
public sealed record CoursePrerequisiteDto(Guid Id, Guid PrerequisiteCourseId, string PrerequisiteCourseTitle,
    string PrerequisiteCourseSlug, bool IsRequired);
public sealed record CourseSkillDto(Guid Id, Guid SkillId, string SkillName, string SkillSlug,
    SkillCategory SkillCategory, SkillProficiencyLevel ProficiencyLevel, bool IsPrimary);
public sealed record CourseListItemDto(Guid Id, string Title, string Slug, string ShortDescription,
    CourseDifficulty Difficulty, int EstimatedDurationMinutes, ContentStatus Status, int LessonCount, int SkillCount);
public sealed record CourseDetailDto(Guid Id, string Title, string Slug, string ShortDescription,
    string? DetailedDescription, CourseDifficulty Difficulty, int EstimatedDurationMinutes, ContentStatus Status,
    IReadOnlyCollection<LessonSummaryDto> Lessons, IReadOnlyCollection<CoursePrerequisiteDto> Prerequisites,
    IReadOnlyCollection<CourseSkillDto> Skills, DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);
