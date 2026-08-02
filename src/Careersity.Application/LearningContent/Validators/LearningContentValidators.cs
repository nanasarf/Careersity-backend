using Careersity.Application.LearningContent.Requests;
using FluentValidation;

namespace Careersity.Application.LearningContent.Validators;

public sealed class CreateSkillRequestValidator : AbstractValidator<CreateSkillRequest>
{ public CreateSkillRequestValidator() { RuleFor(x => x.Name).NotEmpty().MaximumLength(150); RuleFor(x => x.Slug).NotEmpty().MaximumLength(170); RuleFor(x => x.Description).MaximumLength(2_000); RuleFor(x => x.Category).IsInEnum(); } }
public sealed class UpdateSkillRequestValidator : AbstractValidator<UpdateSkillRequest>
{ public UpdateSkillRequestValidator() { RuleFor(x => x.Name).NotEmpty().MaximumLength(150); RuleFor(x => x.Slug).NotEmpty().MaximumLength(170); RuleFor(x => x.Description).MaximumLength(2_000); RuleFor(x => x.Category).IsInEnum(); } }
public sealed class CreateCourseRequestValidator : AbstractValidator<CreateCourseRequest>
{ public CreateCourseRequestValidator() { RuleFor(x => x.Title).NotEmpty().MaximumLength(200); RuleFor(x => x.Slug).NotEmpty().MaximumLength(220); RuleFor(x => x.ShortDescription).NotEmpty().MaximumLength(500); RuleFor(x => x.DetailedDescription).MaximumLength(5_000); RuleFor(x => x.Difficulty).IsInEnum(); RuleFor(x => x.EstimatedDurationMinutes).GreaterThan(0); } }
public sealed class UpdateCourseRequestValidator : AbstractValidator<UpdateCourseRequest>
{ public UpdateCourseRequestValidator() { RuleFor(x => x.Title).NotEmpty().MaximumLength(200); RuleFor(x => x.Slug).NotEmpty().MaximumLength(220); RuleFor(x => x.ShortDescription).NotEmpty().MaximumLength(500); RuleFor(x => x.DetailedDescription).MaximumLength(5_000); RuleFor(x => x.Difficulty).IsInEnum(); RuleFor(x => x.EstimatedDurationMinutes).GreaterThan(0); } }
public sealed class AddLessonRequestValidator : AbstractValidator<AddLessonRequest>
{ public AddLessonRequestValidator() { RuleFor(x => x.Title).NotEmpty().MaximumLength(200); RuleFor(x => x.Slug).NotEmpty().MaximumLength(220); RuleFor(x => x.Summary).MaximumLength(1_000); RuleFor(x => x.Content).MaximumLength(50_000); RuleFor(x => x.ContentType).IsInEnum(); RuleFor(x => x.ExternalResourceUrl).MaximumLength(2_000); RuleFor(x => x.EstimatedDurationMinutes).GreaterThan(0); RuleFor(x => x.Order).GreaterThanOrEqualTo(0); } }
public sealed class UpdateLessonRequestValidator : AbstractValidator<UpdateLessonRequest>
{ public UpdateLessonRequestValidator() { RuleFor(x => x.Title).NotEmpty().MaximumLength(200); RuleFor(x => x.Slug).NotEmpty().MaximumLength(220); RuleFor(x => x.Summary).MaximumLength(1_000); RuleFor(x => x.Content).MaximumLength(50_000); RuleFor(x => x.ContentType).IsInEnum(); RuleFor(x => x.ExternalResourceUrl).MaximumLength(2_000); RuleFor(x => x.EstimatedDurationMinutes).GreaterThan(0); RuleFor(x => x.Order).GreaterThanOrEqualTo(0); } }
public sealed class ReorderLessonsRequestValidator : AbstractValidator<ReorderLessonsRequest>
{ public ReorderLessonsRequestValidator() { RuleFor(x => x.Lessons).NotEmpty(); RuleForEach(x => x.Lessons).ChildRules(x => { x.RuleFor(y => y.LessonId).NotEmpty(); x.RuleFor(y => y.Order).GreaterThanOrEqualTo(0); }); } }
public sealed class AddCoursePrerequisiteRequestValidator : AbstractValidator<AddCoursePrerequisiteRequest>
{ public AddCoursePrerequisiteRequestValidator() => RuleFor(x => x.PrerequisiteCourseId).NotEmpty(); }
public sealed class AddCourseSkillRequestValidator : AbstractValidator<AddCourseSkillRequest>
{ public AddCourseSkillRequestValidator() { RuleFor(x => x.SkillId).NotEmpty(); RuleFor(x => x.ProficiencyLevel).IsInEnum(); } }
public sealed class UpdateCourseSkillRequestValidator : AbstractValidator<UpdateCourseSkillRequest>
{ public UpdateCourseSkillRequestValidator() => RuleFor(x => x.ProficiencyLevel).IsInEnum(); }
