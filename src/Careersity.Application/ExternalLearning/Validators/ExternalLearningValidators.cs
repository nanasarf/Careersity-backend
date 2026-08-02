using Careersity.Application.ExternalLearning.Requests;
using FluentValidation;

namespace Careersity.Application.ExternalLearning.Validators;

internal static class Rules
{
    internal static IRuleBuilderOptions<T, string?> HttpUrl<T>(this IRuleBuilder<T, string?> rule) => rule.Must(x => string.IsNullOrWhiteSpace(x) || Uri.TryCreate(x, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https").WithMessage("URL must be an absolute HTTP or HTTPS URL.").MaximumLength(2000);
}
public sealed class CreateLearningProviderRequestValidator : AbstractValidator<CreateLearningProviderRequest>
{ public CreateLearningProviderRequestValidator() { RuleFor(x => x.Name).NotEmpty().MaximumLength(200); RuleFor(x => x.Slug).NotEmpty().MaximumLength(220); RuleFor(x => x.Description).MaximumLength(3000); RuleFor(x => x.WebsiteUrl).HttpUrl(); RuleFor(x => x.LogoUrl).HttpUrl(); } }
public sealed class UpdateLearningProviderRequestValidator : AbstractValidator<UpdateLearningProviderRequest>
{ public UpdateLearningProviderRequestValidator() { RuleFor(x => x.Name).NotEmpty().MaximumLength(200); RuleFor(x => x.Slug).NotEmpty().MaximumLength(220); RuleFor(x => x.Description).MaximumLength(3000); RuleFor(x => x.WebsiteUrl).HttpUrl(); RuleFor(x => x.LogoUrl).HttpUrl(); } }
public sealed class CreateInstructorRequestValidator : AbstractValidator<CreateInstructorRequest>
{ public CreateInstructorRequestValidator() { RuleFor(x => x.LearningProviderId).NotEmpty(); RuleFor(x => x.Name).NotEmpty().MaximumLength(200); RuleFor(x => x.Title).MaximumLength(200); RuleFor(x => x.Biography).MaximumLength(3000); RuleFor(x => x.ProfileUrl).HttpUrl(); } }
public sealed class UpdateInstructorRequestValidator : AbstractValidator<UpdateInstructorRequest>
{ public UpdateInstructorRequestValidator() { RuleFor(x => x.Name).NotEmpty().MaximumLength(200); RuleFor(x => x.Title).MaximumLength(200); RuleFor(x => x.Biography).MaximumLength(3000); RuleFor(x => x.ProfileUrl).HttpUrl(); } }
public sealed class ChangeInstructorProviderRequestValidator : AbstractValidator<ChangeInstructorProviderRequest> { public ChangeInstructorProviderRequestValidator() => RuleFor(x => x.LearningProviderId).NotEmpty(); }
public sealed class CreateExternalLearningResourceRequestValidator : AbstractValidator<CreateExternalLearningResourceRequest>
{ public CreateExternalLearningResourceRequestValidator() { RuleFor(x => x.LearningProviderId).NotEmpty(); RuleFor(x => x.InstructorId).NotEqual(Guid.Empty); RuleFor(x => x.Title).NotEmpty().MaximumLength(300); RuleFor(x => x.Description).MaximumLength(5000); RuleFor(x => x.Url).NotEmpty().HttpUrl(); RuleFor(x => x.SourceLabel).MaximumLength(300); RuleFor(x => x.EstimatedDurationMinutes).GreaterThan(0).When(x => x.EstimatedDurationMinutes.HasValue); } }
public sealed class UpdateExternalLearningResourceRequestValidator : AbstractValidator<UpdateExternalLearningResourceRequest>
{ public UpdateExternalLearningResourceRequestValidator() { RuleFor(x => x.LearningProviderId).NotEmpty(); RuleFor(x => x.InstructorId).NotEqual(Guid.Empty); RuleFor(x => x.Title).NotEmpty().MaximumLength(300); RuleFor(x => x.Description).MaximumLength(5000); RuleFor(x => x.Url).NotEmpty().HttpUrl(); RuleFor(x => x.SourceLabel).MaximumLength(300); RuleFor(x => x.EstimatedDurationMinutes).GreaterThan(0).When(x => x.EstimatedDurationMinutes.HasValue); } }
public sealed class AssignExternalResourceToCourseRequestValidator : AbstractValidator<AssignExternalResourceToCourseRequest>
{ public AssignExternalResourceToCourseRequestValidator() { RuleFor(x => x.ExternalLearningResourceId).NotEmpty(); RuleFor(x => x.Order).GreaterThanOrEqualTo(0); RuleFor(x => x.Notes).MaximumLength(2000); } }
public sealed class UpdateCourseExternalResourceRequestValidator : AbstractValidator<UpdateCourseExternalResourceRequest>
{ public UpdateCourseExternalResourceRequestValidator() { RuleFor(x => x.Order).GreaterThanOrEqualTo(0); RuleFor(x => x.Notes).MaximumLength(2000); } }
public sealed class ReorderCourseExternalResourcesRequestValidator : AbstractValidator<ReorderCourseExternalResourcesRequest>
{ public ReorderCourseExternalResourcesRequestValidator() { RuleFor(x => x.Resources).NotNull(); RuleForEach(x => x.Resources).ChildRules(x => { x.RuleFor(i => i.AssignmentId).NotEmpty(); x.RuleFor(i => i.Order).GreaterThanOrEqualTo(0); }); RuleFor(x => x.Resources).Must(x => x.Select(i => i.AssignmentId).Distinct().Count()==x.Count && x.Select(i=>i.Order).Distinct().Count()==x.Count).WithMessage("Assignment IDs and orders must be unique."); } }
