namespace Careersity.Application.YouTube.Requests;

public sealed record YouTubeSearchRequest(
    string Query,
    int MaxResults = 10,
    string? PageToken = null,
    bool IncludeDuration = true);
