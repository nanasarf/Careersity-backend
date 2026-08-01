using Careersity.Application.Abstractions.Persistence;
using Careersity.Application.Common.Exceptions;
using Careersity.Domain.Careers;
using Careersity.Domain.Courses;
using Careersity.Domain.Skills;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Careersity.Infrastructure.Persistence;

internal sealed class UnavailableCareersityDbContext : ICareersityDbContext
{
    private static ServiceUnavailableException Error() => new(
        "Career catalog persistence is unavailable. Configure ConnectionStrings:CareersityDatabase.");

    public DbSet<CareerCategory> CareerCategories => throw Error();
    public DbSet<Career> Careers => throw Error();
    public DbSet<CareerSkill> CareerSkills => throw Error();
    public DbSet<CareerPathway> CareerPathways => throw Error();
    public DbSet<PathwayLevel> PathwayLevels => throw Error();
    public DbSet<PathwayLevelCourse> PathwayLevelCourses => throw Error();
    public DbSet<Skill> Skills => throw Error();
    public DbSet<Course> Courses => throw Error();
    public DatabaseFacade Database => throw Error();
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => throw Error();
}
