using Careersity.Domain.Enums;

namespace Careersity.Application.LearningProgress.Requests;

public sealed record EnrollInCareerRequest(Guid CareerId);
public sealed record EnrollmentListQuery(EnrollmentStatus? Status = null, bool IncludeHistory = false, int Page = 1, int PageSize = 20)
{ public int ValidatedPage => Math.Max(Page, 1); public int ValidatedPageSize => Math.Clamp(PageSize, 1, 100); }
