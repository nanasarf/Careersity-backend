using Careersity.Domain.Common;
using Careersity.Domain.Enums;
using Careersity.Domain.Exceptions;

namespace Careersity.Domain.LearningResources;

public sealed class LearningProvider : PublishableEntity
{
    private LearningProvider() { }
    public LearningProvider(string name, string slug, string? description = null, string? websiteUrl = null, string? logoUrl = null) =>
        SetDetails(name, slug, description, websiteUrl, logoUrl);

    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? WebsiteUrl { get; private set; }
    public string? LogoUrl { get; private set; }

    public void UpdateDetails(string name, string slug, string? description, string? websiteUrl, string? logoUrl)
    { EnsureDraft(); SetDetails(name, slug, description, websiteUrl, logoUrl); MarkUpdated(); }
    private void SetDetails(string name, string slug, string? description, string? websiteUrl, string? logoUrl)
    {
        Name = Guard.Required(name, 200, nameof(name)); Slug = Guard.Slug(slug, 220, nameof(slug));
        Description = Guard.Optional(description, 3_000, nameof(description));
        WebsiteUrl = LearningResourceGuards.OptionalHttpUrl(websiteUrl, nameof(websiteUrl));
        LogoUrl = LearningResourceGuards.OptionalHttpUrl(logoUrl, nameof(logoUrl));
    }
    private void EnsureDraft() { if (Status != ContentStatus.Draft) throw new DomainException("Published or archived providers are immutable."); }
}
