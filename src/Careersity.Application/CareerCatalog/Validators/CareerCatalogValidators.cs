using Careersity.Application.CareerCatalog.Requests;
using FluentValidation;

namespace Careersity.Application.CareerCatalog.Validators;

public sealed class CreateCareerCategoryRequestValidator : AbstractValidator<CreateCareerCategoryRequest>
{
    public CreateCareerCategoryRequestValidator() { RuleFor(x => x.Name).NotEmpty().MaximumLength(100); RuleFor(x => x.Slug).NotEmpty().MaximumLength(120); RuleFor(x => x.Description).MaximumLength(1_000); }
}
public sealed class UpdateCareerCategoryRequestValidator : AbstractValidator<UpdateCareerCategoryRequest>
{
    public UpdateCareerCategoryRequestValidator() { RuleFor(x => x.Name).NotEmpty().MaximumLength(100); RuleFor(x => x.Slug).NotEmpty().MaximumLength(120); RuleFor(x => x.Description).MaximumLength(1_000); }
}
public sealed class CreateCareerRequestValidator : AbstractValidator<CreateCareerRequest>
{
    public CreateCareerRequestValidator() { RuleFor(x => x.CareerCategoryId).NotEmpty(); RuleFor(x => x.Title).NotEmpty().MaximumLength(150); RuleFor(x => x.Slug).NotEmpty().MaximumLength(170); RuleFor(x => x.ShortDescription).NotEmpty().MaximumLength(500); RuleFor(x => x.DetailedDescription).MaximumLength(5_000); RuleFor(x => x.Responsibilities).MaximumLength(5_000); RuleFor(x => x.EstimatedDurationWeeks).GreaterThan(0).When(x => x.EstimatedDurationWeeks.HasValue); }
}
public sealed class UpdateCareerRequestValidator : AbstractValidator<UpdateCareerRequest>
{
    public UpdateCareerRequestValidator() { RuleFor(x => x.Title).NotEmpty().MaximumLength(150); RuleFor(x => x.Slug).NotEmpty().MaximumLength(170); RuleFor(x => x.ShortDescription).NotEmpty().MaximumLength(500); RuleFor(x => x.DetailedDescription).MaximumLength(5_000); RuleFor(x => x.Responsibilities).MaximumLength(5_000); RuleFor(x => x.EstimatedDurationWeeks).GreaterThan(0).When(x => x.EstimatedDurationWeeks.HasValue); }
}
public sealed class ChangeCareerCategoryRequestValidator : AbstractValidator<ChangeCareerCategoryRequest>
{ public ChangeCareerCategoryRequestValidator() => RuleFor(x => x.CareerCategoryId).NotEmpty(); }
public sealed class AssignCareerSkillRequestValidator : AbstractValidator<AssignCareerSkillRequest>
{ public AssignCareerSkillRequestValidator() { RuleFor(x => x.SkillId).NotEmpty(); RuleFor(x => x.RequiredProficiencyLevel).IsInEnum(); RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0); } }
public sealed class UpdateCareerSkillRequestValidator : AbstractValidator<UpdateCareerSkillRequest>
{ public UpdateCareerSkillRequestValidator() { RuleFor(x => x.RequiredProficiencyLevel).IsInEnum(); RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0); } }
public sealed class CreateCareerPathwayRequestValidator : AbstractValidator<CreateCareerPathwayRequest>
{ public CreateCareerPathwayRequestValidator() { RuleFor(x => x.CareerId).NotEmpty(); RuleFor(x => x.Name).NotEmpty().MaximumLength(200); RuleFor(x => x.Description).MaximumLength(2_000); RuleFor(x => x.Version).NotEmpty().MaximumLength(30); } }
public sealed class UpdateCareerPathwayRequestValidator : AbstractValidator<UpdateCareerPathwayRequest>
{ public UpdateCareerPathwayRequestValidator() { RuleFor(x => x.Name).NotEmpty().MaximumLength(200); RuleFor(x => x.Description).MaximumLength(2_000); RuleFor(x => x.Version).NotEmpty().MaximumLength(30); } }
public sealed class AddPathwayLevelRequestValidator : AbstractValidator<AddPathwayLevelRequest>
{ public AddPathwayLevelRequestValidator() { RuleFor(x => x.Name).NotEmpty().MaximumLength(150); RuleFor(x => x.Description).MaximumLength(1_500); RuleFor(x => x.Order).GreaterThanOrEqualTo(0); } }
public sealed class UpdatePathwayLevelRequestValidator : AbstractValidator<UpdatePathwayLevelRequest>
{ public UpdatePathwayLevelRequestValidator() { RuleFor(x => x.Name).NotEmpty().MaximumLength(150); RuleFor(x => x.Description).MaximumLength(1_500); RuleFor(x => x.Order).GreaterThanOrEqualTo(0); } }
public sealed class AddPathwayLevelCourseRequestValidator : AbstractValidator<AddPathwayLevelCourseRequest>
{ public AddPathwayLevelCourseRequestValidator() { RuleFor(x => x.CourseId).NotEmpty(); RuleFor(x => x.Order).GreaterThanOrEqualTo(0); } }
public sealed class UpdatePathwayLevelCourseRequestValidator : AbstractValidator<UpdatePathwayLevelCourseRequest>
{ public UpdatePathwayLevelCourseRequestValidator() => RuleFor(x => x.Order).GreaterThanOrEqualTo(0); }
public sealed class ReorderPathwayLevelsRequestValidator : AbstractValidator<ReorderPathwayLevelsRequest>
{
    public ReorderPathwayLevelsRequestValidator() { RuleFor(x => x.Levels).NotEmpty(); RuleForEach(x => x.Levels).ChildRules(item => { item.RuleFor(x => x.LevelId).NotEmpty(); item.RuleFor(x => x.Order).GreaterThanOrEqualTo(0); }); }
}
public sealed class ReorderPathwayCoursesRequestValidator : AbstractValidator<ReorderPathwayCoursesRequest>
{
    public ReorderPathwayCoursesRequestValidator() { RuleFor(x => x.Courses).NotEmpty(); RuleForEach(x => x.Courses).ChildRules(item => { item.RuleFor(x => x.AssignmentId).NotEmpty(); item.RuleFor(x => x.Order).GreaterThanOrEqualTo(0); }); }
}
