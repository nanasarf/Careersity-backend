using Careersity.Domain.Common;
using Careersity.Domain.Exceptions;

namespace Careersity.Domain.Careers;

/// <summary>A stage in a career pathway containing ordered course relationships.</summary>
public sealed class PathwayLevel : AuditableEntity
{
    private readonly List<PathwayLevelCourse> _courses = [];

    private PathwayLevel() { }

    public PathwayLevel(Guid careerPathwayId, string name, int order, string? description = null)
    {
        CareerPathwayId = Guard.NotEmpty(careerPathwayId, nameof(careerPathwayId));
        Name = Guard.Required(name, 150, nameof(name));
        Description = Guard.Optional(description, 1_500, nameof(description));
        Order = Guard.NonNegative(order, nameof(order));
    }

    public Guid CareerPathwayId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int Order { get; private set; }
    public IReadOnlyCollection<PathwayLevelCourse> Courses => _courses.AsReadOnly();

    public void UpdateDetails(string name, string? description = null)
    {
        Name = Guard.Required(name, 150, nameof(name));
        Description = Guard.Optional(description, 1_500, nameof(description));
        MarkUpdated();
    }

    public PathwayLevelCourse AddCourse(Guid courseId, int order, bool isRequired = true)
    {
        Guard.NotEmpty(courseId, nameof(courseId));
        Guard.NonNegative(order, nameof(order));
        if (_courses.Any(x => x.CourseId == courseId)) throw new DomainException("The course is already in this level.");
        if (_courses.Any(x => x.Order == order)) throw new DomainException("Course order must be unique within a level.");
        var relationship = new PathwayLevelCourse(Id, courseId, order, isRequired);
        _courses.Add(relationship);
        MarkUpdated();
        return relationship;
    }

    public void RemoveCourse(Guid courseId)
    {
        var relationship = _courses.SingleOrDefault(x => x.CourseId == courseId)
            ?? throw new DomainException("The course does not belong to this level.");
        _courses.Remove(relationship);
        MarkUpdated();
    }

    public void ReorderCourse(Guid courseId, int newOrder)
    {
        Guard.NonNegative(newOrder, nameof(newOrder));
        var relationship = _courses.SingleOrDefault(x => x.CourseId == courseId)
            ?? throw new DomainException("The course does not belong to this level.");
        if (_courses.Any(x => x.CourseId != courseId && x.Order == newOrder)) throw new DomainException("Course order must be unique within a level.");
        relationship.ChangeOrder(newOrder);
        MarkUpdated();
    }

    internal void ChangeOrder(int newOrder)
    {
        Order = Guard.NonNegative(newOrder, nameof(newOrder));
        MarkUpdated();
    }
}
