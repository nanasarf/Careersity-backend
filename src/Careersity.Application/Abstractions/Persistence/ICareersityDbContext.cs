using Careersity.Domain.Careers;
using Careersity.Domain.Courses;
using Careersity.Domain.Skills;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Careersity.Application.Abstractions.Persistence;

public interface ICareersityDbContext
{
    DbSet<CareerCategory> CareerCategories { get; }
    DbSet<Career> Careers { get; }
    DbSet<CareerSkill> CareerSkills { get; }
    DbSet<CareerPathway> CareerPathways { get; }
    DbSet<PathwayLevel> PathwayLevels { get; }
    DbSet<PathwayLevelCourse> PathwayLevelCourses { get; }
    DbSet<Skill> Skills { get; }
    DbSet<Course> Courses { get; }
    DatabaseFacade Database { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
