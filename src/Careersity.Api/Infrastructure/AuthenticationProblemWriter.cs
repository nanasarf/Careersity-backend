using Microsoft.AspNetCore.Mvc;

namespace Careersity.Api.Infrastructure;

internal static class AuthenticationProblemWriter
{
    public static Task WriteAsync(HttpContext context, int status, string title, string detail)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        var problem = new ProblemDetails
        {
            Type = $"https://httpstatuses.com/{status}", Title = title, Status = status,
            Detail = detail, Instance = context.Request.Path
        };
        problem.Extensions["traceId"] = context.TraceIdentifier;
        return context.Response.WriteAsJsonAsync(problem);
    }
}
