namespace Careersity.Application.YouTube.Dtos;

public sealed record YouTubeSearchResultDto(
    string Title,
    string Channel,
    string ThumbnailUrl,
    string Url,
    DateTimeOffset PublishedAtUtc,
    int? DurationSeconds,
    string Excerpt);

public sealed record YouTubeSearchResponseDto(
    IReadOnlyList<YouTubeSearchResultDto> Items,
    string? NextPageToken);
