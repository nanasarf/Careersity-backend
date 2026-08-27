using System.Net;
using System.Text;
using Careersity.Application.Common.Exceptions;
using Careersity.Application.YouTube.Requests;
using Careersity.Infrastructure.YouTube;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Careersity.IntegrationTests.ExternalLearning;

public sealed class YouTubeSearchServiceTests
{
    [Fact]
    public async Task SearchAsync_MapsSanitizesEnrichesAndCachesResults()
    {
        var handler = new QueueHandler(
            Json("""{"nextPageToken":"NEXT_1","items":[{"id":{"videoId":"abcDEF123_-"},"snippet":{"title":"C&#35; &lt;b&gt;Course&lt;/b&gt;","description":"First\nlesson\u0001","channelTitle":"Example University","publishedAt":"2026-08-01T12:00:00Z","thumbnails":{"medium":{"url":"https://i.ytimg.com/vi/abcDEF123_-/mqdefault.jpg"}}}}]}"""),
            Json("""{"items":[{"id":"abcDEF123_-","contentDetails":{"duration":"PT1H2M3S"}}]}"""));
        var service = CreateService(handler);
        var request = new YouTubeSearchRequest("  C#   full course  ");

        var first = await service.SearchAsync(request, CancellationToken.None);
        var second = await service.SearchAsync(request, CancellationToken.None);

        first.Should().BeSameAs(second);
        handler.Requests.Should().HaveCount(2);
        handler.Requests.Should().OnlyContain(x => x.ApiKey == "test-key");
        handler.Requests[0].Uri.Query.Should().Contain("q=C%23%20full%20course").And.NotContain("test-key");
        first.NextPageToken.Should().Be("NEXT_1");
        first.Items.Should().ContainSingle();
        first.Items[0].Should().BeEquivalentTo(new
        {
            Title = "C# Course", Channel = "Example University",
            ThumbnailUrl = "https://i.ytimg.com/vi/abcDEF123_-/mqdefault.jpg",
            Url = "https://www.youtube.com/watch?v=abcDEF123_-",
            PublishedAtUtc = DateTimeOffset.Parse("2026-08-01T12:00:00Z"),
            DurationSeconds = 3723, Excerpt = "First lesson"
        });
    }

    [Fact]
    public async Task SearchAsync_WhenProviderRejectsRequest_ReturnsSafeUnavailableError()
    {
        var service = CreateService(new QueueHandler(new HttpResponseMessage(HttpStatusCode.Forbidden)
        { Content = new StringContent("secret provider details") }));

        var action = () => service.SearchAsync(new YouTubeSearchRequest("course"), CancellationToken.None);

        var exception = await action.Should().ThrowAsync<ExternalServiceUnavailableException>();
        exception.Which.Message.Should().Be("YouTube search is temporarily unavailable.");
    }

    [Fact]
    public async Task SearchAsync_WhenNotConfigured_DoesNotCallProvider()
    {
        var handler = new QueueHandler();
        var service = CreateService(handler, string.Empty);

        var action = () => service.SearchAsync(new YouTubeSearchRequest("course"), CancellationToken.None);

        await action.Should().ThrowAsync<ExternalServiceUnavailableException>()
            .WithMessage("YouTube search is not configured.");
        handler.Requests.Should().BeEmpty();
    }

    private static YouTubeSearchService CreateService(QueueHandler handler, string apiKey = "test-key")
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://www.googleapis.com/youtube/v3/") };
        var cache = new MemoryCache(new MemoryCacheOptions());
        return new YouTubeSearchService(client, cache,
            Options.Create(new YouTubeOptions { ApiKey = apiKey }), NullLogger<YouTubeSearchService>.Instance);
    }

    private static HttpResponseMessage Json(string body) => new(HttpStatusCode.OK)
    { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private sealed class QueueHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> responses = new(responses);
        public List<(Uri Uri, string? ApiKey)> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add((request.RequestUri!, request.Headers.TryGetValues("x-goog-api-key", out var values) ? values.Single() : null));
            if (responses.Count == 0) throw new InvalidOperationException("Unexpected provider request.");
            return Task.FromResult(responses.Dequeue());
        }
    }
}
