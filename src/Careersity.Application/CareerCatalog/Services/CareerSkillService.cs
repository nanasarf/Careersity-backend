using Careersity.Application.Abstractions.Persistence;
using Careersity.Application.CareerCatalog.Dtos;
using Careersity.Application.CareerCatalog.Requests;
using Careersity.Application.Common.Exceptions;
using Careersity.Domain.Careers;
using Careersity.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Careersity.Application.CareerCatalog.Services;

public sealed class CareerSkillService(ICareersityDbContext db) : ICareerSkillService
{
    public async Task<CareerSkillDto> AssignAsync(Guid careerId, AssignCareerSkillRequest request, CancellationToken cancellationToken)
    {
        await RequireCareerAsync(careerId, cancellationToken);
        var skill = await db.Skills.SingleOrDefaultAsync(x => x.Id == request.SkillId, cancellationToken)
            ?? throw new NotFoundException("Skill was not found.");
        if (await db.CareerSkills.AnyAsync(x => x.CareerId == careerId && x.SkillId == request.SkillId, cancellationToken))
            throw new ConflictException("This skill is already assigned to the career.");
        if (await db.CareerSkills.AnyAsync(x => x.CareerId == careerId && x.DisplayOrder == request.DisplayOrder, cancellationToken))
            throw new ConflictException("Career skill display order must be unique.");
        var assignment = new CareerSkill(careerId, request.SkillId, request.RequiredProficiencyLevel, request.IsRequired, request.DisplayOrder);
        db.CareerSkills.Add(assignment); await db.SaveChangesAsync(cancellationToken);
        return ToDto(assignment, skill);
    }

    public async Task<CareerSkillDto> UpdateAsync(Guid careerId, Guid careerSkillId, UpdateCareerSkillRequest request, CancellationToken cancellationToken)
    {
        await RequireCareerAsync(careerId, cancellationToken);
        var assignment = await db.CareerSkills.SingleOrDefaultAsync(x => x.Id == careerSkillId && x.CareerId == careerId, cancellationToken)
            ?? throw new NotFoundException("Career skill assignment was not found.");
        if (await db.CareerSkills.AnyAsync(x => x.CareerId == careerId && x.DisplayOrder == request.DisplayOrder && x.Id != careerSkillId, cancellationToken))
            throw new ConflictException("Career skill display order must be unique.");
        assignment.UpdateRequirement(request.RequiredProficiencyLevel, request.IsRequired, request.DisplayOrder);
        await db.SaveChangesAsync(cancellationToken);
        var skill = await db.Skills.AsNoTracking().SingleAsync(x => x.Id == assignment.SkillId, cancellationToken);
        return ToDto(assignment, skill);
    }

    public async Task RemoveAsync(Guid careerId, Guid careerSkillId, CancellationToken cancellationToken)
    {
        var assignment = await db.CareerSkills.SingleOrDefaultAsync(x => x.Id == careerSkillId && x.CareerId == careerId, cancellationToken)
            ?? throw new NotFoundException("Career skill assignment was not found.");
        db.CareerSkills.Remove(assignment); await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<CareerSkillDto>> ListAsync(Guid careerId, bool publishedOnly, CancellationToken cancellationToken)
    {
        await RequireCareerAsync(careerId, cancellationToken);
        return await CareerCatalogProjections.LoadSkillsAsync(db, careerId, publishedOnly, cancellationToken);
    }

    private async Task RequireCareerAsync(Guid careerId, CancellationToken cancellationToken)
    {
        if (!await db.Careers.AnyAsync(x => x.Id == careerId, cancellationToken)) throw new NotFoundException("Career was not found.");
    }

    private static CareerSkillDto ToDto(CareerSkill assignment, Domain.Skills.Skill skill) =>
        new(assignment.Id, skill.Id, skill.Name, skill.Slug, skill.Category, assignment.RequiredProficiencyLevel,
            assignment.IsRequired, assignment.DisplayOrder);
}
