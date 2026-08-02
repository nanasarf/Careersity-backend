using Careersity.Domain.Common;

namespace Careersity.Domain.LearningResources;

public sealed class CourseExternalResource : AuditableEntity
{
    private CourseExternalResource() { }
    public CourseExternalResource(Guid courseId, Guid externalLearningResourceId, int order, bool isRequired, string? notes = null)
    {
        CourseId = Guard.NotEmpty(courseId, nameof(courseId)); ExternalLearningResourceId = Guard.NotEmpty(externalLearningResourceId, nameof(externalLearningResourceId));
        Order = Guard.NonNegative(order, nameof(order)); IsRequired = isRequired; Notes = Guard.Optional(notes, 2_000, nameof(notes));
    }
    public Guid CourseId { get; private set; }
    public Guid ExternalLearningResourceId { get; private set; }
    public int Order { get; private set; }
    public bool IsRequired { get; private set; }
    public string? Notes { get; private set; }
    public void Update(int order, bool isRequired, string? notes) { Order = Guard.NonNegative(order, nameof(order)); IsRequired = isRequired; Notes = Guard.Optional(notes, 2_000, nameof(notes)); MarkUpdated(); }
    public void ChangeOrder(int order) { Order = Guard.NonNegative(order, nameof(order)); MarkUpdated(); }
}
