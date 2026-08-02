using Careersity.Domain.Common;

namespace Careersity.Domain.Learning;

public sealed class ExternalResourceProgress : AuditableEntity
{
    private ExternalResourceProgress() { }
    internal ExternalResourceProgress(Guid courseProgressId, Guid courseExternalResourceId)
    {
        CourseProgressId = Guard.NotEmpty(courseProgressId, nameof(courseProgressId));
        CourseExternalResourceId = Guard.NotEmpty(courseExternalResourceId, nameof(courseExternalResourceId));
        StartedAtUtc = LastAccessedAtUtc = DateTimeOffset.UtcNow;
    }
    public Guid CourseProgressId { get; private set; }
    public Guid CourseExternalResourceId { get; private set; }
    public DateTimeOffset StartedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public DateTimeOffset LastAccessedAtUtc { get; private set; }
    public void RecordAccess() { LastAccessedAtUtc = DateTimeOffset.UtcNow; MarkUpdated(); }
    public void Complete() { if (CompletedAtUtc is null) { CompletedAtUtc = DateTimeOffset.UtcNow; LastAccessedAtUtc = CompletedAtUtc.Value; MarkUpdated(); } }
}
