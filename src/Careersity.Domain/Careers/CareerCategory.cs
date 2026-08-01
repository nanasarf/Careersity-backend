using Careersity.Domain.Common;

namespace Careersity.Domain.Careers;

/// <summary>Groups careers into a discoverable subject area.</summary>
public sealed class CareerCategory : PublishableEntity
{
    private CareerCategory() { }

    public CareerCategory(string name, string slug, string? description = null) =>
        SetDetails(name, slug, description);

    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    public void UpdateDetails(string name, string slug, string? description = null)
    {
        SetDetails(name, slug, description);
        MarkUpdated();
    }

    private void SetDetails(string name, string slug, string? description)
    {
        Name = Guard.Required(name, 100, nameof(name));
        Slug = Guard.Slug(slug, 120, nameof(slug));
        Description = Guard.Optional(description, 1_000, nameof(description));
    }
}
