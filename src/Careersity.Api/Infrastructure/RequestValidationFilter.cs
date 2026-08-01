using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Careersity.Api.Infrastructure;

public sealed class RequestValidationFilter(IServiceProvider services) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var failures = new List<FluentValidation.Results.ValidationFailure>();
        foreach (var argument in context.ActionArguments.Values.Where(x => x is not null))
        {
            var modelType = argument!.GetType();
            var validatorType = typeof(IValidator<>).MakeGenericType(modelType);
            var validator = services.GetService(validatorType);
            if (validator is null) continue;
            var method = validatorType.GetMethod(nameof(IValidator<object>.ValidateAsync), [modelType, typeof(CancellationToken)])!;
            var task = (Task<FluentValidation.Results.ValidationResult>)method.Invoke(
                validator, [argument, context.HttpContext.RequestAborted])!;
            failures.AddRange((await task).Errors.Where(x => x is not null));
        }
        if (failures.Count > 0) throw new ValidationException(failures);
        await next();
    }
}
