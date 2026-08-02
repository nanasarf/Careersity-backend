using Careersity.Application.CurriculumActivities.Requests;
using FluentValidation;

namespace Careersity.Application.CurriculumActivities.Validators;

public sealed class CreateAssessmentRequestValidator : AbstractValidator<CreateAssessmentRequest>
{ public CreateAssessmentRequestValidator() { RuleFor(x => x.CourseId).NotEmpty(); RuleFor(x => x.Title).NotEmpty().MaximumLength(200); RuleFor(x => x.Description).MaximumLength(2_000); RuleFor(x => x.PassingScorePercentage).InclusiveBetween(1, 100); RuleFor(x => x.MaximumAttempts).GreaterThan(0).When(x => x.MaximumAttempts.HasValue); } }
public sealed class UpdateAssessmentRequestValidator : AbstractValidator<UpdateAssessmentRequest>
{ public UpdateAssessmentRequestValidator() { RuleFor(x => x.Title).NotEmpty().MaximumLength(200); RuleFor(x => x.Description).MaximumLength(2_000); RuleFor(x => x.PassingScorePercentage).InclusiveBetween(1, 100); RuleFor(x => x.MaximumAttempts).GreaterThan(0).When(x => x.MaximumAttempts.HasValue); } }
public sealed class AddQuestionRequestValidator : AbstractValidator<AddQuestionRequest>
{ public AddQuestionRequestValidator() { RuleFor(x => x.Prompt).NotEmpty().MaximumLength(2_000); RuleFor(x => x.QuestionType).IsInEnum(); RuleFor(x => x.Order).GreaterThanOrEqualTo(0); RuleFor(x => x.Points).GreaterThan(0); } }
public sealed class UpdateQuestionRequestValidator : AbstractValidator<UpdateQuestionRequest>
{ public UpdateQuestionRequestValidator() { RuleFor(x => x.Prompt).NotEmpty().MaximumLength(2_000); RuleFor(x => x.QuestionType).IsInEnum(); RuleFor(x => x.Order).GreaterThanOrEqualTo(0); RuleFor(x => x.Points).GreaterThan(0); } }
public sealed class ReorderQuestionsRequestValidator : AbstractValidator<ReorderQuestionsRequest>
{ public ReorderQuestionsRequestValidator() { RuleFor(x => x.Questions).NotEmpty(); RuleForEach(x => x.Questions).ChildRules(x => { x.RuleFor(y => y.QuestionId).NotEmpty(); x.RuleFor(y => y.Order).GreaterThanOrEqualTo(0); }); } }
public sealed class AddAnswerOptionRequestValidator : AbstractValidator<AddAnswerOptionRequest>
{ public AddAnswerOptionRequestValidator() { RuleFor(x => x.Text).NotEmpty().MaximumLength(1_000); RuleFor(x => x.Order).GreaterThanOrEqualTo(0); } }
public sealed class UpdateAnswerOptionRequestValidator : AbstractValidator<UpdateAnswerOptionRequest>
{ public UpdateAnswerOptionRequestValidator() { RuleFor(x => x.Text).NotEmpty().MaximumLength(1_000); RuleFor(x => x.Order).GreaterThanOrEqualTo(0); } }
public sealed class ReorderAnswerOptionsRequestValidator : AbstractValidator<ReorderAnswerOptionsRequest>
{ public ReorderAnswerOptionsRequestValidator() { RuleFor(x => x.AnswerOptions).NotEmpty(); RuleForEach(x => x.AnswerOptions).ChildRules(x => { x.RuleFor(y => y.AnswerOptionId).NotEmpty(); x.RuleFor(y => y.Order).GreaterThanOrEqualTo(0); }); } }
public sealed class CreateProjectRequestValidator : AbstractValidator<CreateProjectRequest>
{ public CreateProjectRequestValidator() { RuleFor(x => x.CourseId).NotEmpty(); RuleFor(x => x.Title).NotEmpty().MaximumLength(200); RuleFor(x => x.Description).NotEmpty().MaximumLength(2_000); RuleFor(x => x.Instructions).NotEmpty().MaximumLength(10_000); RuleFor(x => x.ExpectedOutput).MaximumLength(3_000); RuleFor(x => x.EvaluationCriteria).MaximumLength(5_000); RuleFor(x => x.SubmissionType).IsInEnum(); RuleFor(x => x.EstimatedDurationMinutes).GreaterThan(0); } }
public sealed class UpdateProjectRequestValidator : AbstractValidator<UpdateProjectRequest>
{ public UpdateProjectRequestValidator() { RuleFor(x => x.Title).NotEmpty().MaximumLength(200); RuleFor(x => x.Description).NotEmpty().MaximumLength(2_000); RuleFor(x => x.Instructions).NotEmpty().MaximumLength(10_000); RuleFor(x => x.ExpectedOutput).MaximumLength(3_000); RuleFor(x => x.EvaluationCriteria).MaximumLength(5_000); RuleFor(x => x.SubmissionType).IsInEnum(); RuleFor(x => x.EstimatedDurationMinutes).GreaterThan(0); } }
