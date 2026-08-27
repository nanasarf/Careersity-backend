using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Careersity.Application.Common.Exceptions;
using Careersity.Application.YouTube.Dtos;
using Careersity.Application.YouTube.Requests;
using Careersity.Application.YouTube.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Careersity.Infrastructure.YouTube;

public sealed partial class YouTubeSearchService(
    HttpClient httpClient,
    IMemoryCache cache,
    IOptions<YouTubeOptions> configuredOptions,
    ILogger<YouTubeSearchService> logger) : IYouTubeSearchService
{
    private readonly YouTubeOptions options = configuredOptions.Value;

    public async Task<YouTubeSearchResponseDto> SearchAsync(
        YouTubeSearchRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            throw new ExternalServiceUnavailableException("YouTube search is not configured.");

        var normalizedQuery = Whitespace().Replace(request.Query.Trim(), " ");
        var cacheKey = $"youtube-search:{normalizedQuery.ToUpperInvariant()}:{request.MaxResults}:{request.PageToken}:{request.IncludeDuration}";
        if (cache.TryGetValue(cacheKey, out YouTubeSearchResponseDto? cached) && cached is not null)
            return cached;

        try
        {
            using var searchRequest = new HttpRequestMessage(HttpMethod.Get, BuildSearchUri(normalizedQuery, request));
            searchRequest.Headers.Add("x-goog-api-key", options.ApiKey);
            using var searchResponse = await httpClient.SendAsync(searchRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            EnsureSuccess(searchResponse);
            var payload = await searchResponse.Content.ReadFromJsonAsync<YouTubeSearchPayload>(cancellationToken: cancellationToken)
                ?? throw new ExternalServiceUnavailableException("YouTube returned an invalid response.");

            var durations = request.IncludeDuration
                ? await GetDurationsAsync(payload.Items.Select(x => x.Id.VideoId).Where(x => !string.IsNullOrWhiteSpace(x))!, cancellationToken)
                : new Dictionary<string, int?>();

            var items = payload.Items
                .Where(x => IsVideoId(x.Id.VideoId) && x.Snippet is not null)
                .Select(x => Map(x, durations))
                .ToArray();
            var result = new YouTubeSearchResponseDto(items, SanitizeToken(payload.NextPageToken));
            cache.Set(cacheKey, result, TimeSpan.FromMinutes(options.CacheDurationMinutes));
            return result;
        }
        catch (ExternalServiceUnavailableException) { throw; }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ExternalServiceUnavailableException("YouTube search timed out. Please try again.");
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "YouTube search transport failure");
            throw new ExternalServiceUnavailableException("YouTube search is temporarily unavailable.");
        }
        catch (Exception exception) when (exception is JsonException or FormatException or OverflowException)
        {
            logger.LogWarning(exception, "YouTube returned an invalid search response");
            throw new ExternalServiceUnavailableException("YouTube returned an invalid response.");
        }
    }

    private static string BuildSearchUri(string query, YouTubeSearchRequest request)
    {
        var values = new Dictionary<string, string>
        {
            ["part"] = "snippet", ["type"] = "video", ["q"] = query,
            ["maxResults"] = request.MaxResults.ToString(CultureInfo.InvariantCulture),
            ["safeSearch"] = "moderate", ["fields"] = "nextPageToken,items(id/videoId,snippet(title,description,channelTitle,publishedAt,thumbnails/medium/url,thumbnails/high/url))"
        };
        if (!string.IsNullOrWhiteSpace(request.PageToken)) values["pageToken"] = request.PageToken;
        return "search?" + string.Join("&", values.Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value)}"));
    }

    private async Task<Dictionary<string, int?>> GetDurationsAsync(IEnumerable<string> videoIds, CancellationToken cancellationToken)
    {
        var ids = videoIds.Where(IsVideoId).Distinct(StringComparer.Ordinal).ToArray();
        if (ids.Length == 0) return [];
        var uri = "videos?part=contentDetails&fields=items(id,contentDetails/duration)&id=" + Uri.EscapeDataString(string.Join(',', ids));
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Add("x-goog-api-key", options.ApiKey);
        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        EnsureSuccess(response);
        var payload = await response.Content.ReadFromJsonAsync<YouTubeVideosPayload>(cancellationToken: cancellationToken)
            ?? throw new ExternalServiceUnavailableException("YouTube returned an invalid response.");
        return payload.Items.Where(x => IsVideoId(x.Id)).ToDictionary(
            x => x.Id!, x => ParseDurationSeconds(x.ContentDetails?.Duration), StringComparer.Ordinal);
    }

    private static void EnsureSuccess(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;
        throw new ExternalServiceUnavailableException(response.StatusCode == HttpStatusCode.TooManyRequests
            ? "YouTube search quota is temporarily unavailable. Please try again later."
            : "YouTube search is temporarily unavailable.");
    }

    private static YouTubeSearchResultDto Map(YouTubeSearchItem item, IReadOnlyDictionary<string, int?> durations)
    {
        var snippet = item.Snippet!;
        var id = item.Id.VideoId!;
        durations.TryGetValue(id, out var duration);
        return new(
            Clean(snippet.Title, 300), Clean(snippet.ChannelTitle, 200),
            SafeThumbnail(snippet.Thumbnails?.High?.Url ?? snippet.Thumbnails?.Medium?.Url),
            $"https://www.youtube.com/watch?v={id}", snippet.PublishedAt,
            duration, Clean(snippet.Description, 500));
    }

    private static string Clean(string? value, int maximumLength)
    {
        var decoded = WebUtility.HtmlDecode(value ?? string.Empty);
        var withoutTags = HtmlTags().Replace(decoded, string.Empty);
        var clean = ControlCharacters().Replace(Whitespace().Replace(withoutTags, " ").Trim(), string.Empty);
        return clean.Length <= maximumLength ? clean : clean[..maximumLength].TrimEnd();
    }

    private static string SafeThumbnail(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps &&
        (uri.Host.Equals("i.ytimg.com", StringComparison.OrdinalIgnoreCase) || uri.Host.EndsWith(".ytimg.com", StringComparison.OrdinalIgnoreCase))
            ? uri.AbsoluteUri : string.Empty;

    private static string? SanitizeToken(string? token) =>
        !string.IsNullOrWhiteSpace(token) && token.Length <= 500 && PageToken().IsMatch(token) ? token : null;

    private static bool IsVideoId(string? id) => !string.IsNullOrWhiteSpace(id) && VideoId().IsMatch(id);

    private static int? ParseDurationSeconds(string? duration)
    {
        if (string.IsNullOrWhiteSpace(duration)) return null;
        try
        {
            var value = System.Xml.XmlConvert.ToTimeSpan(duration);
            return value.TotalSeconds is >= 0 and <= int.MaxValue ? (int)Math.Round(value.TotalSeconds) : null;
        }
        catch (FormatException) { return null; }
    }

    [GeneratedRegex("\\s+")] private static partial Regex Whitespace();
    [GeneratedRegex("<[^>]*>")] private static partial Regex HtmlTags();
    [GeneratedRegex("[\\x00-\\x08\\x0B\\x0C\\x0E-\\x1F\\x7F]")] private static partial Regex ControlCharacters();
    [GeneratedRegex("^[A-Za-z0-9_-]{11}$")] private static partial Regex VideoId();
    [GeneratedRegex("^[A-Za-z0-9_-]+$")] private static partial Regex PageToken();

    private sealed record YouTubeSearchPayload(
        [property: JsonPropertyName("items")] YouTubeSearchItem[] Items,
        [property: JsonPropertyName("nextPageToken")] string? NextPageToken);
    private sealed record YouTubeSearchItem(
        [property: JsonPropertyName("id")] YouTubeSearchId Id,
        [property: JsonPropertyName("snippet")] YouTubeSnippet? Snippet);
    private sealed record YouTubeSearchId([property: JsonPropertyName("videoId")] string? VideoId);
    private sealed record YouTubeSnippet(
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("description")] string? Description,
        [property: JsonPropertyName("channelTitle")] string? ChannelTitle,
        [property: JsonPropertyName("publishedAt")] DateTimeOffset PublishedAt,
        [property: JsonPropertyName("thumbnails")] YouTubeThumbnails? Thumbnails);
    private sealed record YouTubeThumbnails(
        [property: JsonPropertyName("medium")] YouTubeThumbnail? Medium,
        [property: JsonPropertyName("high")] YouTubeThumbnail? High);
    private sealed record YouTubeThumbnail([property: JsonPropertyName("url")] string? Url);
    private sealed record YouTubeVideosPayload([property: JsonPropertyName("items")] YouTubeVideoItem[] Items);
    private sealed record YouTubeVideoItem(
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("contentDetails")] YouTubeContentDetails? ContentDetails);
    private sealed record YouTubeContentDetails([property: JsonPropertyName("duration")] string? Duration);
}
