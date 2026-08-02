using Careersity.Application.LearningProgress.Requests;
using FluentValidation;

namespace Careersity.Application.LearningProgress.Validators;

public sealed class EnrollInCareerRequestValidator : AbstractValidator<EnrollInCareerRequest>
{
    public EnrollInCareerRequestValidator() => RuleFor(x => x.CareerId).NotEmpty();
}
