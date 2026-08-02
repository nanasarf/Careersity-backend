using Careersity.Domain.Enums;

namespace Careersity.Application.LearningContent.Requests;

public sealed record CreateSkillRequest(string Name, string Slug, string? Description, SkillCategory Category);
public sealed record UpdateSkillRequest(string Name, string Slug, string? Description, SkillCategory Category);
public sealed record SkillQuery(int Page = 1, int PageSize = 20, string? Search = null,
    SkillCategory? Category = null, ContentStatus? Status = null)
{ public int ValidatedPage => Page < 1 ? 1 : Page; public int ValidatedPageSize => Math.Clamp(PageSize, 1, 100); }
public sealed record CreateCourseRequest(string Title, string Slug, string ShortDescription,
    string? DetailedDescription, CourseDifficulty Difficulty, int EstimatedDurationMinutes);
public sealed record UpdateCourseRequest(string Title, string Slug, string ShortDescription,
    string? DetailedDescription, CourseDifficulty Difficulty, int EstimatedDurationMinutes);
public sealed record CourseQuery(int Page = 1, int PageSize = 20, string? Search = null,
    CourseDifficulty? Difficulty = null, ContentStatus? Status = null, Guid? SkillId = null)
{ public int ValidatedPage => Page < 1 ? 1 : Page; public int ValidatedPageSize => Math.Clamp(PageSize, 1, 100); }
public sealed record AddLessonRequest(string Title, string Slug, string? Summary, string? Content,
    LessonContentType ContentType, string? ExternalResourceUrl, int EstimatedDurationMinutes, int Order, bool IsRequired);
public sealed record UpdateLessonRequest(string Title, string Slug, string? Summary, string? Content,
    LessonContentType ContentType, string? ExternalResourceUrl, int EstimatedDurationMinutes, int Order, bool IsRequired);
public sealed record LessonOrderItem(Guid LessonId, int Order);
public sealed record ReorderLessonsRequest(IReadOnlyCollection<LessonOrderItem> Lessons);
public sealed record AddCoursePrerequisiteRequest(Guid PrerequisiteCourseId, bool IsRequired);
public sealed record UpdateCoursePrerequisiteRequest(bool IsRequired);
public sealed record AddCourseSkillRequest(Guid SkillId, SkillProficiencyLevel ProficiencyLevel, bool IsPrimary);
public sealed record UpdateCourseSkillRequest(SkillProficiencyLevel ProficiencyLevel, bool IsPrimary);
