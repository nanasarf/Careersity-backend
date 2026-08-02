using Careersity.Domain.Common;
using Careersity.Domain.Exceptions;

namespace Careersity.Domain.Learning;

public sealed class CourseProgress : AuditableEntity
{
    private readonly List<LessonProgress> _lessonProgressRecords = [];
    private CourseProgress() { }

    internal CourseProgress(Guid careerEnrollmentId, Guid courseId)
    {
        CareerEnrollmentId = Guard.NotEmpty(careerEnrollmentId, nameof(careerEnrollmentId));
        CourseId = Guard.NotEmpty(courseId, nameof(courseId));
        StartedAtUtc = LastAccessedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid CareerEnrollmentId { get; private set; }
    public Guid CourseId { get; private set; }
    public DateTimeOffset StartedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public DateTimeOffset LastAccessedAtUtc { get; private set; }
    public IReadOnlyCollection<LessonProgress> LessonProgressRecords => _lessonProgressRecords.AsReadOnly();

    public void RecordAccess() { LastAccessedAtUtc = DateTimeOffset.UtcNow; MarkUpdated(); }
    public void Complete() { if (CompletedAtUtc is null) { CompletedAtUtc = DateTimeOffset.UtcNow; LastAccessedAtUtc = CompletedAtUtc.Value; MarkUpdated(); } }
    public LessonProgress AddLessonProgress(Guid lessonId)
    {
        var existing = _lessonProgressRecords.SingleOrDefault(x => x.LessonId == lessonId);
        if (existing is not null) return existing;
        var progress = new LessonProgress(Id, lessonId); _lessonProgressRecords.Add(progress); RecordAccess(); return progress;
    }
}
