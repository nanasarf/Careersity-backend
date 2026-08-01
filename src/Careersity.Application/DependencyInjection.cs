using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Careersity.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddCareersityApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
