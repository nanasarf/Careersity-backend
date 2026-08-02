using Careersity.Domain.Enums;

namespace Careersity.Application.CurriculumImports.Requests;

public sealed record ImportCategory(string Name, string Slug, string? Description);
public sealed record ImportCareer(string Title, string Slug, string ShortDescription, int? EstimatedDurationWeeks);
public sealed record ImportSkill(string Name, string Slug, SkillCategory Category, string? Description = null);
public sealed record ImportProvider(string Name, string Slug, string? WebsiteUrl = null, string? Description = null);
public sealed record ImportInstructor(string ProviderSlug, string Name, string? Title = null, string? ProfileUrl = null);
public sealed record ImportCourseSkill(string SkillSlug, SkillProficiencyLevel ProficiencyLevel, bool IsPrimary);
public sealed record ImportLesson(string Title, string Slug, LessonContentType ContentType, int EstimatedDurationMinutes, int Order, bool IsRequired, string? Summary = null, string? ExternalResourceUrl = null);
public sealed record ImportExternalResource(string ProviderSlug, string Title, ExternalResourceType ResourceType,
    ResourceAccessType AccessType, string Url, int Order, bool IsRequired, string? InstructorName = null, int? EstimatedDurationMinutes = null);
public sealed record ImportCourse(string Title, string Slug, CourseDifficulty Difficulty, int EstimatedDurationMinutes,
    IReadOnlyCollection<ImportCourseSkill> Skills, IReadOnlyCollection<ImportLesson> Lessons,
    IReadOnlyCollection<ImportExternalResource> ExternalResources, string? ShortDescription = null);
public sealed record ImportLevelCourse(string CourseSlug, int Order, bool IsRequired);
public sealed record ImportLevel(string Name, int Order, IReadOnlyCollection<ImportLevelCourse> Courses, string? Description = null);
public sealed record ImportPathway(string Name, string Version, bool IsPrimary, IReadOnlyCollection<ImportLevel> Levels, string? Description = null);
public sealed record CurriculumImportRequest(ImportCategory Category, ImportCareer Career,
    IReadOnlyCollection<ImportSkill> Skills, IReadOnlyCollection<ImportProvider> Providers,
    IReadOnlyCollection<ImportInstructor>? Instructors, IReadOnlyCollection<ImportCourse> Courses, ImportPathway Pathway,
    string Mode = "CreateOnly");
