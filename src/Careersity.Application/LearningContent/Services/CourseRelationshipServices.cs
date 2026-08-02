using Careersity.Application.Abstractions.Persistence;
using Careersity.Application.Common.Exceptions;
using Careersity.Application.LearningContent.Dtos;
using Careersity.Application.LearningContent.Requests;
using Careersity.Domain.Courses;
using Microsoft.EntityFrameworkCore;

namespace Careersity.Application.LearningContent.Services;

public sealed class CoursePrerequisiteService(ICareersityDbContext db) : ICoursePrerequisiteService
{
    public async Task<CoursePrerequisiteDto> AddAsync(Guid courseId, AddCoursePrerequisiteRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var course = await FindCourseAsync(courseId, cancellationToken); SkillService.RequireDraft(course.Status, "course");
        var prerequisite = await db.Courses.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.PrerequisiteCourseId, cancellationToken)
            ?? throw new NotFoundException("Prerequisite course was not found.");
        if (courseId == prerequisite.Id) throw new ConflictException("A course cannot be its own prerequisite.");
        if (course.Prerequisites.Any(x => x.PrerequisiteCourseId == prerequisite.Id)) throw new ConflictException("The prerequisite is already associated with this course.");
        if (await CreatesCycleAsync(courseId, prerequisite.Id, cancellationToken)) throw new ConflictException("The prerequisite would create a circular course dependency.");
        var relation = course.AddPrerequisite(prerequisite.Id, request.IsRequired); db.CoursePrerequisites.Add(relation); await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new(relation.Id, prerequisite.Id, prerequisite.Title, prerequisite.Slug, relation.IsRequired);
    }
    public async Task<CoursePrerequisiteDto> UpdateAsync(Guid courseId, Guid relationshipId, UpdateCoursePrerequisiteRequest request, CancellationToken cancellationToken)
    {
        var course = await FindCourseAsync(courseId, cancellationToken); SkillService.RequireDraft(course.Status, "course");
        var relation = course.Prerequisites.SingleOrDefault(x => x.Id == relationshipId) ?? throw new NotFoundException("Course prerequisite was not found.");
        relation.SetRequired(request.IsRequired); await db.SaveChangesAsync(cancellationToken);
        var prerequisite = await db.Courses.AsNoTracking().SingleAsync(x => x.Id == relation.PrerequisiteCourseId, cancellationToken);
        return new(relation.Id, prerequisite.Id, prerequisite.Title, prerequisite.Slug, relation.IsRequired);
    }
    public async Task RemoveAsync(Guid courseId, Guid relationshipId, CancellationToken cancellationToken)
    {
        var course = await FindCourseAsync(courseId, cancellationToken); SkillService.RequireDraft(course.Status, "course");
        var relation = course.Prerequisites.SingleOrDefault(x => x.Id == relationshipId) ?? throw new NotFoundException("Course prerequisite was not found.");
        course.RemovePrerequisite(relation.PrerequisiteCourseId); await db.SaveChangesAsync(cancellationToken);
    }
    private async Task<bool> CreatesCycleAsync(Guid courseId, Guid proposedPrerequisiteId, CancellationToken token)
    {
        var edges = await db.CoursePrerequisites.AsNoTracking().Select(x => new { x.CourseId, x.PrerequisiteCourseId }).ToListAsync(token);
        var adjacency = edges.GroupBy(x => x.CourseId).ToDictionary(x => x.Key, x => x.Select(y => y.PrerequisiteCourseId).ToArray());
        var pending = new Stack<Guid>(); var visited = new HashSet<Guid>(); pending.Push(proposedPrerequisiteId);
        while (pending.Count > 0) { var current = pending.Pop(); if (current == courseId) return true; if (!visited.Add(current)) continue; if (adjacency.TryGetValue(current, out var next)) foreach (var id in next) pending.Push(id); }
        return false;
    }
    private async Task<Course> FindCourseAsync(Guid id, CancellationToken token) => await db.Courses.Include(x => x.Prerequisites)
        .SingleOrDefaultAsync(x => x.Id == id, token) ?? throw new NotFoundException("Course was not found.");
}

public sealed class CourseSkillService(ICareersityDbContext db) : ICourseSkillService
{
    public async Task<CourseSkillDto> AddAsync(Guid courseId, AddCourseSkillRequest request, CancellationToken cancellationToken)
    {
        var course = await FindCourseAsync(courseId, cancellationToken); SkillService.RequireDraft(course.Status, "course");
        var skill = await db.Skills.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.SkillId, cancellationToken) ?? throw new NotFoundException("Skill was not found.");
        if (course.CourseSkills.Any(x => x.SkillId == skill.Id)) throw new ConflictException("The skill is already associated with this course.");
        var relation = course.AssociateSkill(skill.Id, request.ProficiencyLevel, request.IsPrimary); db.CourseSkills.Add(relation); await db.SaveChangesAsync(cancellationToken);
        return new(relation.Id, skill.Id, skill.Name, skill.Slug, skill.Category, relation.ProficiencyLevel, relation.IsPrimary);
    }
    public async Task<CourseSkillDto> UpdateAsync(Guid courseId, Guid relationshipId, UpdateCourseSkillRequest request, CancellationToken cancellationToken)
    {
        var course = await FindCourseAsync(courseId, cancellationToken); SkillService.RequireDraft(course.Status, "course");
        var relation = course.CourseSkills.SingleOrDefault(x => x.Id == relationshipId) ?? throw new NotFoundException("Course skill was not found.");
        relation.Update(request.ProficiencyLevel, request.IsPrimary); await db.SaveChangesAsync(cancellationToken);
        var skill = await db.Skills.AsNoTracking().SingleAsync(x => x.Id == relation.SkillId, cancellationToken);
        return new(relation.Id, skill.Id, skill.Name, skill.Slug, skill.Category, relation.ProficiencyLevel, relation.IsPrimary);
    }
    public async Task RemoveAsync(Guid courseId, Guid relationshipId, CancellationToken cancellationToken)
    {
        var course = await FindCourseAsync(courseId, cancellationToken); SkillService.RequireDraft(course.Status, "course");
        var relation = course.CourseSkills.SingleOrDefault(x => x.Id == relationshipId) ?? throw new NotFoundException("Course skill was not found.");
        course.RemoveSkillAssociation(relation.SkillId); await db.SaveChangesAsync(cancellationToken);
    }
    private async Task<Course> FindCourseAsync(Guid id, CancellationToken token) => await db.Courses.Include(x => x.CourseSkills)
        .SingleOrDefaultAsync(x => x.Id == id, token) ?? throw new NotFoundException("Course was not found.");
}
