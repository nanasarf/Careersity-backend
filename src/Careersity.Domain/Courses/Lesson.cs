using Careersity.Domain.Common;
using Careersity.Domain.Enums;

namespace Careersity.Domain.Courses;

/// <summary>An ordered unit of learning content within a course.</summary>
public sealed class Lesson : AuditableEntity
{
    private Lesson() { }

    public Lesson(Guid courseId, string title, string slug, LessonContentType contentType,
        int estimatedDurationMinutes, int order, string? summary = null, string? content = null,
        string? externalResourceUrl = null, bool isRequired = true)
    {
        CourseId = Guard.NotEmpty(courseId, nameof(courseId));
        SetMetadata(title, slug, contentType, estimatedDurationMinutes, summary, content, externalResourceUrl);
        Order = Guard.NonNegative(order, nameof(order));
        IsRequired = isRequired;
    }

    public Guid CourseId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Summary { get; private set; }
    public string? Content { get; private set; }
    public LessonContentType ContentType { get; private set; }
    public string? ExternalResourceUrl { get; private set; }
    public int EstimatedDurationMinutes { get; private set; }
    public int Order { get; private set; }
    public bool IsRequired { get; private set; }

    public void UpdateContentAndMetadata(string title, string slug, LessonContentType contentType,
        int estimatedDurationMinutes, string? summary = null, string? content = null, string? externalResourceUrl = null)
    {
        SetMetadata(title, slug, contentType, estimatedDurationMinutes, summary, content, externalResourceUrl);
        MarkUpdated();
    }

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

    private void SetMetadata(string title, string slug, LessonContentType contentType,
        int estimatedDurationMinutes, string? summary, string? content, string? externalResourceUrl)
    {
        var normalizedUrl = Guard.Optional(externalResourceUrl, 2_000, nameof(externalResourceUrl));
        if (contentType == LessonContentType.ExternalResource && normalizedUrl is null)
            throw new ArgumentException("An external-resource lesson requires a URL.", nameof(externalResourceUrl));
        if (normalizedUrl is not null && (!Uri.TryCreate(normalizedUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)))
            throw new ArgumentException("The external resource URL must be an absolute HTTP or HTTPS URL.", nameof(externalResourceUrl));

        Title = Guard.Required(title, 200, nameof(title));
        Slug = Guard.Slug(slug, 220, nameof(slug));
        Summary = Guard.Optional(summary, 1_000, nameof(summary));
        Content = Guard.Optional(content, 50_000, nameof(content));
        ContentType = contentType;
        ExternalResourceUrl = normalizedUrl;
        EstimatedDurationMinutes = Guard.Positive(estimatedDurationMinutes, nameof(estimatedDurationMinutes));
    }
}
