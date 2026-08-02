using Careersity.Domain.Enums;

namespace Careersity.Application.ExternalLearning.Requests;

public sealed record CreateLearningProviderRequest(string Name, string Slug, string? Description, string? WebsiteUrl, string? LogoUrl);
public sealed record UpdateLearningProviderRequest(string Name, string Slug, string? Description, string? WebsiteUrl, string? LogoUrl);
public sealed record CreateInstructorRequest(Guid LearningProviderId, string Name, string? Title, string? Biography, string? ProfileUrl);
public sealed record UpdateInstructorRequest(string Name, string? Title, string? Biography, string? ProfileUrl);
public sealed record ChangeInstructorProviderRequest(Guid LearningProviderId);
public sealed record CreateExternalLearningResourceRequest(Guid LearningProviderId, Guid? InstructorId, string Title, string? Description, ExternalResourceType ResourceType, ResourceAccessType AccessType, string Url, string? SourceLabel, int? EstimatedDurationMinutes);
public sealed record UpdateExternalLearningResourceRequest(Guid LearningProviderId, Guid? InstructorId, string Title, string? Description, ExternalResourceType ResourceType, ResourceAccessType AccessType, string Url, string? SourceLabel, int? EstimatedDurationMinutes);
public sealed record AssignExternalResourceToCourseRequest(Guid ExternalLearningResourceId, int Order, bool IsRequired, string? Notes);
public sealed record UpdateCourseExternalResourceRequest(int Order, bool IsRequired, string? Notes);
public sealed record CourseExternalResourceOrderItem(Guid AssignmentId, int Order);
public sealed record ReorderCourseExternalResourcesRequest(IReadOnlyCollection<CourseExternalResourceOrderItem> Resources);
public sealed record ExternalLearningQuery(int Page = 1, int PageSize = 20, string? Search = null, ContentStatus? Status = null)
{ public int ValidatedPage => Page < 1 ? 1 : Page; public int ValidatedPageSize => Math.Clamp(PageSize, 1, 100); }
