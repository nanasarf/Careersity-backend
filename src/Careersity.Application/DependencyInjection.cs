using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Careersity.Application.CareerCatalog.Services;
using Careersity.Application.Identity.Services;
using Careersity.Application.LearningContent.Services;

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
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IUserProfileService, UserProfileService>();
        services.AddScoped<ISkillService, SkillService>();
        services.AddScoped<ICourseService, CourseService>();
        services.AddScoped<ILessonService, LessonService>();
        services.AddScoped<ICoursePrerequisiteService, CoursePrerequisiteService>();
        services.AddScoped<ICourseSkillService, CourseSkillService>();

        return services;
    }
}
