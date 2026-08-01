using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Careersity.Application.CareerCatalog.Services;

namespace Careersity.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddCareersityApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<ICareerCategoryService, CareerCategoryService>();
        services.AddScoped<ICareerService, CareerService>();
        services.AddScoped<ICareerSkillService, CareerSkillService>();
        services.AddScoped<ICareerPathwayService, CareerPathwayService>();

        return services;
    }
}
