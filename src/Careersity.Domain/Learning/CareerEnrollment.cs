using Careersity.Domain.Common;
using Careersity.Domain.Enums;
using Careersity.Domain.Exceptions;

namespace Careersity.Domain.Learning;

/// <summary>A learner's durable enrollment in one specific career-pathway version.</summary>
public sealed class CareerEnrollment : AuditableEntity
{
    private readonly List<CourseProgress> _courseProgressRecords = [];
    private CareerEnrollment() { }

    public CareerEnrollment(Guid userId, Guid careerId, Guid careerPathwayId)
    {
        UserId = Guard.NotEmpty(userId, nameof(userId));
        CareerId = Guard.NotEmpty(careerId, nameof(careerId));
        CareerPathwayId = Guard.NotEmpty(careerPathwayId, nameof(careerPathwayId));
        Status = EnrollmentStatus.Active;
        EnrolledAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid UserId { get; private set; }
    public Guid CareerId { get; private set; }
    public Guid CareerPathwayId { get; private set; }
    public EnrollmentStatus Status { get; private set; }
    public DateTimeOffset EnrolledAtUtc { get; private set; }
    public DateTimeOffset? StartedAtUtc { get; private set; }
    public DateTimeOffset? PausedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public DateTimeOffset? WithdrawnAtUtc { get; private set; }
    public IReadOnlyCollection<CourseProgress> CourseProgressRecords => _courseProgressRecords.AsReadOnly();

    public void Pause()
    {
        if (Status != EnrollmentStatus.Active) throw new DomainException("Only an active enrollment can be paused.");
        Status = EnrollmentStatus.Paused; PausedAtUtc = DateTimeOffset.UtcNow; MarkUpdated();
    }

    public void Resume()
    {
        if (Status != EnrollmentStatus.Paused) throw new DomainException("Only a paused enrollment can be resumed.");
        Status = EnrollmentStatus.Active; PausedAtUtc = null; MarkUpdated();
    }

    public void Withdraw()
    {
        if (Status is not (EnrollmentStatus.Active or EnrollmentStatus.Paused)) throw new DomainException("Only an active or paused enrollment can be withdrawn.");
        Status = EnrollmentStatus.Withdrawn; WithdrawnAtUtc = DateTimeOffset.UtcNow; MarkUpdated();
    }

    public void Complete()
    {
        if (Status == EnrollmentStatus.Completed) return;
        if (Status != EnrollmentStatus.Active) throw new DomainException("Only an active enrollment can be completed.");
        Status = EnrollmentStatus.Completed; CompletedAtUtc = DateTimeOffset.UtcNow; MarkUpdated();
    }

    public CourseProgress AddCourseProgress(Guid courseId)
    {
        EnsureActivityAllowed();
        var existing = _courseProgressRecords.SingleOrDefault(x => x.CourseId == courseId);
        if (existing is not null) return existing;
        var progress = new CourseProgress(Id, courseId);
        _courseProgressRecords.Add(progress);
        StartedAtUtc ??= progress.StartedAtUtc;
        MarkUpdated();
        return progress;
    }

    public void EnsureActivityAllowed()
    {
        if (Status != EnrollmentStatus.Active) throw new DomainException("Learning activity requires an active enrollment.");
    }
}
