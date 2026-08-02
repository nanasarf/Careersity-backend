using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Careersity.Application.CareerCatalog.Services;
using Careersity.Application.Identity.Services;
using Careersity.Application.LearningContent.Services;
using Careersity.Application.CurriculumActivities.Services;
using Careersity.Application.LearningProgress.Services;
using Careersity.Application.AssessmentAttempts.Services;
using Careersity.Application.ExternalLearning.Services;
using Careersity.Application.CurriculumImports.Services;

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
        services.AddScoped<IAssessmentService, AssessmentService>();
        services.AddScoped<IQuestionService, QuestionService>();
        services.AddScoped<IAnswerOptionService, AnswerOptionService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<ILearningProgressService, LearningProgressService>();
        services.AddScoped<ICourseCompletionEvaluator, CourseCompletionEvaluator>();
        services.AddScoped<IAssessmentAttemptService, AssessmentAttemptService>();
        services.AddScoped<ILearningProviderService, LearningProviderService>();
        services.AddScoped<IInstructorService, InstructorService>();
        services.AddScoped<IExternalLearningResourceService, ExternalLearningResourceService>();
        services.AddScoped<ICourseExternalResourceService, CourseExternalResourceService>();
        services.AddScoped<ILearnerExternalResourceProgressService, LearnerExternalResourceProgressService>();
        services.AddScoped<ICurriculumImportService, CurriculumImportService>();

        return services;
    }
}
