using Careersity.Domain.Common;

namespace Careersity.Domain.Courses;

/// <summary>Declares another course that should precede the owning course.</summary>
public sealed class CoursePrerequisite : AuditableEntity
{
    private CoursePrerequisite() { }

    public CoursePrerequisite(Guid courseId, Guid prerequisiteCourseId, bool isRequired = true)
    {
        CourseId = Guard.NotEmpty(courseId, nameof(courseId));
        PrerequisiteCourseId = Guard.NotEmpty(prerequisiteCourseId, nameof(prerequisiteCourseId));
        if (CourseId == PrerequisiteCourseId) throw new ArgumentException("A course cannot be its own prerequisite.", nameof(prerequisiteCourseId));
        IsRequired = isRequired;
    }

    public Guid CourseId { get; private set; }
    public Guid PrerequisiteCourseId { get; private set; }
    public bool IsRequired { get; private set; }

    public void SetRequired(bool isRequired)
    {
        IsRequired = isRequired;
        MarkUpdated();
    }
}
