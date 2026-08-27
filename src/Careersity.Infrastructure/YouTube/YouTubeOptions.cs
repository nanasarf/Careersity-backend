namespace Careersity.Infrastructure.YouTube;

public sealed class YouTubeOptions
{
    public const string SectionName = "YouTube";
    public string ApiKey { get; init; } = string.Empty;
    public int CacheDurationMinutes { get; init; } = 15;
    public int RequestTimeoutSeconds { get; init; } = 10;
}
