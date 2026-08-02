namespace Careersity.Domain.LearningResources;

internal static class LearningResourceGuards
{
    internal static string? OptionalHttpUrl(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return HttpUrl(value, name);
    }

    internal static string HttpUrl(string value, string name)
    {
        var trimmed = value.Trim();
        if (trimmed.Length > 2_000) throw new ArgumentException("URL cannot exceed 2000 characters.", name);
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new ArgumentException("URL must be an absolute HTTP or HTTPS URL.", name);
        return trimmed;
    }
}
