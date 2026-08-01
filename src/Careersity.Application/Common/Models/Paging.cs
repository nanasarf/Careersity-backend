namespace Careersity.Application.Common.Models;

public sealed record PagedRequest(int Page = 1, int PageSize = 20, string? Search = null)
{
    public int ValidatedPage => Page < 1 ? 1 : Page;
    public int ValidatedPageSize => Math.Clamp(PageSize, 1, 100);
}

public sealed record PagedResult<T>(IReadOnlyCollection<T> Items, int Page, int PageSize, int TotalCount);
