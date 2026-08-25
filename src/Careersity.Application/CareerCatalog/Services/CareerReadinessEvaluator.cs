using Careersity.Application.Abstractions.Persistence;
using Careersity.Application.CareerCatalog.Dtos;
using Careersity.Application.Common.Exceptions;
using Careersity.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Careersity.Application.CareerCatalog.Services;

public sealed class CareerReadinessEvaluator(ICareersityDbContext db)
{
    public async Task<CareerReadinessDto> EvaluateAsync(Guid careerId, CancellationToken token)
    {
        var career = await db.Careers.AsNoTracking().SingleOrDefaultAsync(x => x.Id == careerId, token)
            ?? throw new NotFoundException("Career was not found.");
        var category = await db.CareerCategories.AsNoTracking().SingleOrDefaultAsync(x => x.Id == career.CareerCategoryId, token);
        var skills = await (from assignment in db.CareerSkills.AsNoTracking()
                            join skill in db.Skills.AsNoTracking() on assignment.SkillId equals skill.Id
                            where assignment.CareerId == careerId
                            select new { assignment, skill }).ToListAsync(token);
        var pathways = await db.CareerPathways.AsNoTracking().Where(x => x.CareerId == careerId && x.IsPrimary)
            .Include(x => x.Levels).ThenInclude(x => x.Courses).ToListAsync(token);
        var primary = pathways.SingleOrDefault();
        var levels = primary?.Levels.OrderBy(x => x.Order).ToList() ?? [];
        var assignments = levels.SelectMany(x => x.Courses).ToList();
        var courseIds = assignments.Select(x => x.CourseId).Distinct().ToArray();
        var courses = await db.Courses.AsNoTracking().Where(x => courseIds.Contains(x.Id))
            .Include(x => x.Lessons).Include(x => x.CourseSkills).Include(x => x.Prerequisites).ToListAsync(token);
        var courseSkillIds = courses.SelectMany(x => x.CourseSkills).Select(x => x.SkillId).Distinct().ToArray();
        var publishedCourseSkillIds = await db.Skills.AsNoTracking().Where(x => courseSkillIds.Contains(x.Id) && x.Status == ContentStatus.Published).Select(x => x.Id).ToListAsync(token);
        var prerequisiteIds = courses.SelectMany(x => x.Prerequisites).Select(x => x.PrerequisiteCourseId).Distinct().ToArray();
        var publishedPrerequisiteIds = await db.Courses.AsNoTracking().Where(x => prerequisiteIds.Contains(x.Id) && x.Status == ContentStatus.Published).Select(x => x.Id).ToListAsync(token);
        var resources = await db.CourseExternalResources.AsNoTracking().Where(x => courseIds.Contains(x.CourseId)).ToListAsync(token);
        var resourceIds = resources.Select(x => x.ExternalLearningResourceId).Distinct().ToArray();
        var publishedResourceIds = await db.ExternalLearningResources.AsNoTracking().Where(x => resourceIds.Contains(x.Id) && x.Status == ContentStatus.Published).Select(x => x.Id).ToListAsync(token);

        static bool Contiguous(IEnumerable<int> orders) { var values = orders.Order().ToArray(); return values.SequenceEqual(Enumerable.Range(0, values.Length)); }
        var checks = new List<CareerReadinessCheckDto>();
        void Add(string code, string label, bool passed, string failure, Guid? entityId = null, string? entityType = null) =>
            checks.Add(new(code, label, passed, true, passed ? null : failure, entityId, entityType));

        Add("CATEGORY_ASSIGNED", "Category assigned", category is not null, "Assign an existing category to the career.", career.CareerCategoryId, "CareerCategory");
        Add("CATEGORY_PUBLISHED", "Category published", category?.Status == ContentStatus.Published, "Publish the career category first.", category?.Id, "CareerCategory");
        Add("CAREER_DETAILS_COMPLETE", "Career details complete", !string.IsNullOrWhiteSpace(career.Title) && !string.IsNullOrWhiteSpace(career.Slug) && !string.IsNullOrWhiteSpace(career.ShortDescription), "Complete the required career details.", career.Id, "Career");
        Add("CAREER_SKILLS_ASSIGNED", "Career skills assigned", skills.Count > 0, "Assign at least one skill to the career.", career.Id, "Career");
        Add("REQUIRED_CAREER_SKILL_EXISTS", "Required career skill exists", skills.Any(x => x.assignment.IsRequired), "Mark at least one career skill as required.", career.Id, "Career");
        Add("CAREER_SKILLS_VALID", "Career skills valid", skills.Count > 0 && skills.All(x => x.skill.Status == ContentStatus.Published) && Contiguous(skills.Select(x => x.assignment.DisplayOrder)), "All career skills must be Published and ordered contiguously from zero.", career.Id, "Career");
        Add("PRIMARY_PATHWAY_EXISTS", "Primary pathway exists", pathways.Count == 1, "Configure exactly one primary pathway.", career.Id, "Career");
        Add("PRIMARY_PATHWAY_VALID", "Primary pathway published", primary?.Status == ContentStatus.Published, "Publish the primary pathway before publishing the career.", primary?.Id, "CareerPathway");
        Add("PATHWAY_LEVEL_EXISTS", "Pathway level exists", levels.Count > 0, "Add at least one pathway level.", primary?.Id, "CareerPathway");
        var ordersValid = levels.Count > 0 && Contiguous(levels.Select(x => x.Order)) && levels.All(x => x.Courses.Count > 0 && Contiguous(x.Courses.Select(c => c.Order)) && x.Courses.Select(c => c.CourseId).Distinct().Count() == x.Courses.Count);
        Add("PATHWAY_ORDERS_VALID", "Pathway ordering valid", ordersValid, "Levels and courses must be unique and ordered contiguously from zero; every level must contain a course.", primary?.Id, "CareerPathway");
        Add("REQUIRED_PATHWAY_COURSE_EXISTS", "Required pathway course exists", assignments.Any(x => x.IsRequired), "Add at least one required course to the pathway.", primary?.Id, "CareerPathway");
        Add("REQUIRED_COURSES_PUBLISHED", "Pathway courses published", assignments.Count > 0 && courses.Count == courseIds.Length && courses.All(x => x.Status == ContentStatus.Published), "Every assigned course, including optional courses, must be Published.", primary?.Id, "CareerPathway");
        var requirementsValid = courses.Count == courseIds.Length && courses.All(course =>
            course.Lessons.Any(x => x.IsRequired) && course.CourseSkills.Count > 0 && course.CourseSkills.Any(x => x.IsPrimary) &&
            course.CourseSkills.All(x => publishedCourseSkillIds.Contains(x.SkillId)) &&
            course.Prerequisites.All(x => publishedPrerequisiteIds.Contains(x.PrerequisiteCourseId)) &&
            resources.Where(x => x.CourseId == course.Id).All(x => publishedResourceIds.Contains(x.ExternalLearningResourceId)));
        Add("COURSE_REQUIREMENTS_VALID", "Course requirements valid", assignments.Count > 0 && requirementsValid, "Every pathway course must retain valid lessons, skills, prerequisites, and external-resource dependencies.", primary?.Id, "CareerPathway");
        return new(careerId, checks.All(x => !x.Blocking || x.Passed), checks);
    }
}
