using Careersity.Domain.Common;

namespace Careersity.Domain.Learning;

public sealed class LessonProgress : AuditableEntity
{
    private LessonProgress() { }
    internal LessonProgress(Guid courseProgressId, Guid lessonId)
    {
        CourseProgressId = Guard.NotEmpty(courseProgressId, nameof(courseProgressId));
        LessonId = Guard.NotEmpty(lessonId, nameof(lessonId));
        StartedAtUtc = LastAccessedAtUtc = DateTimeOffset.UtcNow;
    }
    public Guid CourseProgressId { get; private set; }
    public Guid LessonId { get; private set; }
    public DateTimeOffset StartedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public DateTimeOffset LastAccessedAtUtc { get; private set; }
    public void RecordAccess() { LastAccessedAtUtc = DateTimeOffset.UtcNow; MarkUpdated(); }
    public void Complete() { if (CompletedAtUtc is null) { CompletedAtUtc = DateTimeOffset.UtcNow; LastAccessedAtUtc = CompletedAtUtc.Value; MarkUpdated(); } }
}
