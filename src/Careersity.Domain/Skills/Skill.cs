using Careersity.Domain.Common;
using Careersity.Domain.Enums;

namespace Careersity.Domain.Skills;

/// <summary>A capability that careers and courses can require or develop.</summary>
public sealed class Skill : PublishableEntity
{
    private Skill() { }

    public Skill(string name, string slug, SkillCategory category, string? description = null)
    {
        SetDetails(name, slug, description);
        Category = category;
    }

    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public SkillCategory Category { get; private set; }

    public void UpdateDetails(string name, string slug, string? description = null)
    {
        SetDetails(name, slug, description);
        MarkUpdated();
    }

    public void ChangeCategory(SkillCategory category)
    {
        Category = category;
        MarkUpdated();
    }

    private void SetDetails(string name, string slug, string? description)
    {
        Name = Guard.Required(name, 150, nameof(name));
        Slug = Guard.Slug(slug, 170, nameof(slug));
        Description = Guard.Optional(description, 2_000, nameof(description));
    }
}
