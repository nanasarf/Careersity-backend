using Careersity.Domain.Common;

namespace Careersity.Domain.Careers;

/// <summary>Describes a career learners can prepare for.</summary>
public sealed class Career : PublishableEntity
{
    private Career() { }

    public Career(Guid careerCategoryId, string title, string slug, string shortDescription,
        string? detailedDescription = null, string? responsibilities = null, int? estimatedDurationWeeks = null)
    {
        CareerCategoryId = Guard.NotEmpty(careerCategoryId, nameof(careerCategoryId));
        SetDetails(title, slug, shortDescription, detailedDescription, responsibilities, estimatedDurationWeeks);
    }

    public Guid CareerCategoryId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string ShortDescription { get; private set; } = string.Empty;
    public string? DetailedDescription { get; private set; }
    public string? Responsibilities { get; private set; }
    public int? EstimatedDurationWeeks { get; private set; }

    public void UpdateBasicDetails(string title, string slug, string shortDescription,
        string? detailedDescription = null, string? responsibilities = null, int? estimatedDurationWeeks = null)
    {
        SetDetails(title, slug, shortDescription, detailedDescription, responsibilities, estimatedDurationWeeks);
        MarkUpdated();
    }

    public void ChangeCategory(Guid careerCategoryId)
    {
        CareerCategoryId = Guard.NotEmpty(careerCategoryId, nameof(careerCategoryId));
        MarkUpdated();
    }

    private void SetDetails(string title, string slug, string shortDescription,
        string? detailedDescription, string? responsibilities, int? estimatedDurationWeeks)
    {
        if (estimatedDurationWeeks.HasValue) Guard.Positive(estimatedDurationWeeks.Value, nameof(estimatedDurationWeeks));
        Title = Guard.Required(title, 150, nameof(title));
        Slug = Guard.Slug(slug, 170, nameof(slug));
        ShortDescription = Guard.Required(shortDescription, 500, nameof(shortDescription));
        DetailedDescription = Guard.Optional(detailedDescription, 5_000, nameof(detailedDescription));
        Responsibilities = Guard.Optional(responsibilities, 5_000, nameof(responsibilities));
        EstimatedDurationWeeks = estimatedDurationWeeks;
    }
}
