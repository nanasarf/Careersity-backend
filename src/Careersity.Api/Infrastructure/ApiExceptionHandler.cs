using Careersity.Application.Common.Exceptions;
using Careersity.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Careersity.Api.Infrastructure;

public sealed class ApiExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            ValidationException or ArgumentException => (StatusCodes.Status400BadRequest, "Request validation failed"),
            RequestValidationException => (StatusCodes.Status400BadRequest, "Request validation failed"),
            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            ConflictException or DomainException or DbUpdateException => (StatusCodes.Status409Conflict, "Request conflicts with current state"),
            AuthenticationFailedException or UnauthorizedException => (StatusCodes.Status401Unauthorized, "Authentication failed"),
            ServiceUnavailableException => (StatusCodes.Status503ServiceUnavailable, "Career catalog unavailable"),
            ExternalServiceUnavailableException => (StatusCodes.Status503ServiceUnavailable, "External service unavailable"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };
        if (status == StatusCodes.Status500InternalServerError) logger.LogError(exception, "Unhandled request exception");

        var details = new ProblemDetails
        {
            Type = $"https://httpstatuses.com/{status}", Title = title, Status = status,
            Detail = exception is DbUpdateException ? "The change conflicts with an existing record or relationship. Refresh and retry." :
                status == 500 ? "An unexpected error occurred." : exception.Message,
            Instance = context.Request.Path
        };
        details.Extensions["traceId"] = context.TraceIdentifier;
        if (exception is ValidationException validation)
        {
            details.Extensions["errors"] = validation.Errors.GroupBy(x => x.PropertyName)
                .ToDictionary(x => x.Key, x => x.Select(error => error.ErrorMessage).Distinct().ToArray());
        }
        context.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        { HttpContext = context, ProblemDetails = details, Exception = exception });
    }
}
