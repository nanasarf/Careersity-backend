using Careersity.Application.Common.Exceptions;
using Careersity.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

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
            ConflictException or DomainException => (StatusCodes.Status409Conflict, "Request conflicts with current state"),
            ServiceUnavailableException => (StatusCodes.Status503ServiceUnavailable, "Career catalog unavailable"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };
        if (status == StatusCodes.Status500InternalServerError) logger.LogError(exception, "Unhandled request exception");

        var details = new ProblemDetails
        {
            Type = $"https://httpstatuses.com/{status}", Title = title, Status = status,
            Detail = status == 500 ? "An unexpected error occurred." : exception.Message,
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
