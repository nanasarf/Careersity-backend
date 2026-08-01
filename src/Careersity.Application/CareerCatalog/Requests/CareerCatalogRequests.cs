using Careersity.Domain.Enums;

namespace Careersity.Application.CareerCatalog.Requests;

public sealed record CreateCareerCategoryRequest(string Name, string Slug, string? Description);
public sealed record UpdateCareerCategoryRequest(string Name, string Slug, string? Description);
public sealed record CreateCareerRequest(Guid CareerCategoryId, string Title, string Slug, string ShortDescription,
    string? DetailedDescription, string? Responsibilities, int? EstimatedDurationWeeks);
public sealed record UpdateCareerRequest(string Title, string Slug, string ShortDescription,
    string? DetailedDescription, string? Responsibilities, int? EstimatedDurationWeeks);
public sealed record ChangeCareerCategoryRequest(Guid CareerCategoryId);
public sealed record AssignCareerSkillRequest(Guid SkillId, SkillProficiencyLevel RequiredProficiencyLevel,
    bool IsRequired, int DisplayOrder);
public sealed record UpdateCareerSkillRequest(SkillProficiencyLevel RequiredProficiencyLevel,
    bool IsRequired, int DisplayOrder);
public sealed record CreateCareerPathwayRequest(Guid CareerId, string Name, string? Description,
    string Version, bool IsPrimary);
public sealed record UpdateCareerPathwayRequest(string Name, string? Description, string Version, bool IsPrimary);
public sealed record AddPathwayLevelRequest(string Name, string? Description, int Order);
public sealed record UpdatePathwayLevelRequest(string Name, string? Description, int Order);
public sealed record AddPathwayLevelCourseRequest(Guid CourseId, int Order, bool IsRequired);
public sealed record UpdatePathwayLevelCourseRequest(int Order, bool IsRequired);
public sealed record LevelOrderItem(Guid LevelId, int Order);
public sealed record ReorderPathwayLevelsRequest(IReadOnlyCollection<LevelOrderItem> Levels);
public sealed record CourseOrderItem(Guid AssignmentId, int Order);
public sealed record ReorderPathwayCoursesRequest(IReadOnlyCollection<CourseOrderItem> Courses);
