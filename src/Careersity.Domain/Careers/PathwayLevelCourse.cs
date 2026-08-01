using Careersity.Domain.Common;

namespace Careersity.Domain.Careers;

/// <summary>Places a course at an ordered position within a pathway level.</summary>
public sealed class PathwayLevelCourse : AuditableEntity
{
    private PathwayLevelCourse() { }

    internal PathwayLevelCourse(Guid pathwayLevelId, Guid courseId, int order, bool isRequired)
    {
        PathwayLevelId = Guard.NotEmpty(pathwayLevelId, nameof(pathwayLevelId));
        CourseId = Guard.NotEmpty(courseId, nameof(courseId));
        Order = Guard.NonNegative(order, nameof(order));
        IsRequired = isRequired;
    }

    public Guid PathwayLevelId { get; private set; }
    public Guid CourseId { get; private set; }
    public int Order { get; private set; }
    public bool IsRequired { get; private set; }

    public void SetRequired(bool isRequired)
    {
        IsRequired = isRequired;
        MarkUpdated();
    }

    internal void ChangeOrder(int order)
    {
        Order = Guard.NonNegative(order, nameof(order));
        MarkUpdated();
    }
}
