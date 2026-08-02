using Careersity.Domain.Common;
using Careersity.Domain.Enums;
using Careersity.Domain.Exceptions;

namespace Careersity.Domain.LearningResources;

public sealed class Instructor : PublishableEntity
{
    private Instructor() { }
    public Instructor(Guid learningProviderId, string name, string? title = null, string? biography = null, string? profileUrl = null)
    { LearningProviderId = Guard.NotEmpty(learningProviderId, nameof(learningProviderId)); SetDetails(name, title, biography, profileUrl); }
    public Guid LearningProviderId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Title { get; private set; }
    public string? Biography { get; private set; }
    public string? ProfileUrl { get; private set; }
    public void UpdateDetails(string name, string? title, string? biography, string? profileUrl)
    { EnsureDraft(); SetDetails(name, title, biography, profileUrl); MarkUpdated(); }
    public void ChangeProvider(Guid providerId) { EnsureDraft(); LearningProviderId = Guard.NotEmpty(providerId, nameof(providerId)); MarkUpdated(); }
    private void SetDetails(string name, string? title, string? biography, string? profileUrl)
    {
        Name = Guard.Required(name, 200, nameof(name)); Title = Guard.Optional(title, 200, nameof(title));
        Biography = Guard.Optional(biography, 3_000, nameof(biography)); ProfileUrl = LearningResourceGuards.OptionalHttpUrl(profileUrl, nameof(profileUrl));
    }
    private void EnsureDraft() { if (Status != ContentStatus.Draft) throw new DomainException("Published or archived instructors are immutable."); }
}
