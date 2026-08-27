using Careersity.Application.YouTube.Dtos;
using Careersity.Application.YouTube.Requests;

namespace Careersity.Application.YouTube.Services;

public interface IYouTubeSearchService
{
    Task<YouTubeSearchResponseDto> SearchAsync(YouTubeSearchRequest request, CancellationToken cancellationToken);
}
