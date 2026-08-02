using Careersity.Domain.Common;
using Careersity.Domain.Enums;
using Careersity.Domain.Exceptions;

namespace Careersity.Domain.LearningResources;

public sealed class ExternalLearningResource : PublishableEntity
{
    private ExternalLearningResource() { }
    public ExternalLearningResource(Guid learningProviderId, Guid? instructorId, string title,
        ExternalResourceType resourceType, ResourceAccessType accessType, string url, string? description = null,
        string? sourceLabel = null, int? estimatedDurationMinutes = null)
    {
        LearningProviderId = Guard.NotEmpty(learningProviderId, nameof(learningProviderId));
        SetInstructor(instructorId); SetMetadata(title, description, resourceType, accessType, url, sourceLabel, estimatedDurationMinutes);
    }
    public Guid LearningProviderId { get; private set; }
    public Guid? InstructorId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public ExternalResourceType ResourceType { get; private set; }
    public ResourceAccessType AccessType { get; private set; }
    public string Url { get; private set; } = string.Empty;
    public string? SourceLabel { get; private set; }
    public int? EstimatedDurationMinutes { get; private set; }
    public DateTimeOffset? LastReviewedAtUtc { get; private set; }
    public void UpdateMetadata(string title, string? description, ExternalResourceType type, ResourceAccessType access,
        string url, string? sourceLabel, int? duration) { EnsureDraft(); SetMetadata(title, description, type, access, url, sourceLabel, duration); MarkUpdated(); }
    public void ChangeProvider(Guid providerId) { EnsureDraft(); LearningProviderId = Guard.NotEmpty(providerId, nameof(providerId)); MarkUpdated(); }
    public void AssignInstructor(Guid? instructorId) { EnsureDraft(); SetInstructor(instructorId); MarkUpdated(); }
    public void MarkReviewed() { if (Status == ContentStatus.Archived) throw new DomainException("Archived resources cannot be reviewed."); LastReviewedAtUtc = DateTimeOffset.UtcNow; MarkUpdated(); }
    private void SetMetadata(string title, string? description, ExternalResourceType type, ResourceAccessType access, string url, string? sourceLabel, int? duration)
    {
        if (duration.HasValue) Guard.Positive(duration.Value, nameof(duration));
        Title = Guard.Required(title, 300, nameof(title)); Description = Guard.Optional(description, 5_000, nameof(description));
        ResourceType = type; AccessType = access; Url = LearningResourceGuards.HttpUrl(url, nameof(url));
        SourceLabel = Guard.Optional(sourceLabel, 300, nameof(sourceLabel)); EstimatedDurationMinutes = duration;
    }
    private void SetInstructor(Guid? id) { if (id == Guid.Empty) throw new ArgumentException("Instructor ID cannot be empty.", nameof(id)); InstructorId = id; }
    private void EnsureDraft() { if (Status != ContentStatus.Draft) throw new DomainException("Published or archived resource metadata is immutable."); }
}
