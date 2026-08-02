using Careersity.Application.AssessmentAttempts.Requests;
using FluentValidation;

namespace Careersity.Application.AssessmentAttempts.Validators;

public sealed class SaveAssessmentResponseRequestValidator : AbstractValidator<SaveAssessmentResponseRequest>
{
    public SaveAssessmentResponseRequestValidator()
    {
        RuleFor(x => x.QuestionId).NotEmpty();
        RuleFor(x => x.SelectedAnswerOptionIds).NotNull();
        RuleFor(x => x.SelectedAnswerOptionIds).Must(x => x is not null && x.All(id => id != Guid.Empty)).WithMessage("Selected option IDs cannot be empty.");
        RuleFor(x => x.SelectedAnswerOptionIds).Must(x => x is not null && x.Distinct().Count() == x.Count).WithMessage("Selected option IDs cannot contain duplicates.");
    }
}
public sealed class SaveAssessmentResponsesRequestValidator : AbstractValidator<SaveAssessmentResponsesRequest>
{
    public SaveAssessmentResponsesRequestValidator()
    {
        RuleFor(x => x.Responses).NotNull(); RuleForEach(x => x.Responses).SetValidator(new SaveAssessmentResponseRequestValidator());
        RuleFor(x => x.Responses).Must(x => x is not null && x.Select(r => r.QuestionId).Distinct().Count() == x.Count).WithMessage("Question IDs cannot be duplicated.");
    }
}
public sealed class SubmitAssessmentAttemptRequestValidator : AbstractValidator<SubmitAssessmentAttemptRequest>
{
    public SubmitAssessmentAttemptRequestValidator()
    {
        When(x => x.Responses is not null, () =>
        {
            RuleForEach(x => x.Responses!).SetValidator(new SaveAssessmentResponseRequestValidator());
            RuleFor(x => x.Responses!).Must(x => x.Select(r => r.QuestionId).Distinct().Count() == x.Count).WithMessage("Question IDs cannot be duplicated.");
            RuleForEach(x => x.Responses!).Must(x => x.SelectedAnswerOptionIds.Count > 0).WithMessage("Submitted answers must select at least one option.");
        });
    }
}
