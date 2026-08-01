using Careersity.Application.Common.Models;
using Careersity.Domain.Enums;

namespace Careersity.Application.CareerCatalog.Dtos;

public sealed record CareerCategoryDto(Guid Id, string Name, string Slug, string? Description,
    ContentStatus Status, DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);

public sealed record CareerListItemDto(Guid Id, Guid CareerCategoryId, string CategoryName, string Title,
    string Slug, string ShortDescription, int? EstimatedDurationWeeks, ContentStatus Status);

public sealed record CareerSkillDto(Guid Id, Guid SkillId, string SkillName, string SkillSlug,
    SkillCategory SkillCategory, SkillProficiencyLevel RequiredProficiencyLevel, bool IsRequired, int DisplayOrder);

public sealed record PathwayLevelCourseDto(Guid Id, Guid CourseId, string CourseTitle, string CourseSlug,
    CourseDifficulty Difficulty, int EstimatedDurationMinutes, int Order, bool IsRequired);

public sealed record PathwayLevelDto(Guid Id, string Name, string? Description, int Order,
    IReadOnlyCollection<PathwayLevelCourseDto> Courses);

public sealed record CareerPathwayDto(Guid Id, Guid CareerId, string Name, string? Description,
    string Version, bool IsPrimary, ContentStatus Status, IReadOnlyCollection<PathwayLevelDto> Levels);

public sealed record CareerDetailDto(Guid Id, CareerCategoryDto CareerCategory, string Title, string Slug,
    string ShortDescription, string? DetailedDescription, string? Responsibilities, int? EstimatedDurationWeeks,
    ContentStatus Status, IReadOnlyCollection<CareerSkillDto> Skills, CareerPathwayDto? PrimaryPathway,
    DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);

public sealed record CareerQuery(int Page = 1, int PageSize = 20, string? Search = null,
    Guid? CategoryId = null, ContentStatus? Status = null)
{
    public int ValidatedPage => Page < 1 ? 1 : Page;
    public int ValidatedPageSize => Math.Clamp(PageSize, 1, 100);
}
