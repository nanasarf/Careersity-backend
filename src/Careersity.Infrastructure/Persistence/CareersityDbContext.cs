using Careersity.Domain.Assessments;
using Careersity.Domain.Careers;
using Careersity.Domain.Courses;
using Careersity.Domain.Projects;
using Careersity.Domain.Skills;
using Microsoft.EntityFrameworkCore;

namespace Careersity.Infrastructure.Persistence;

/// <summary>EF Core unit of persistence for Careersity learning content.</summary>
public sealed class CareersityDbContext(DbContextOptions<CareersityDbContext> options) : DbContext(options)
{
    public DbSet<CareerCategory> CareerCategories => Set<CareerCategory>();
    public DbSet<Career> Careers => Set<Career>();
    public DbSet<CareerSkill> CareerSkills => Set<CareerSkill>();
    public DbSet<CareerPathway> CareerPathways => Set<CareerPathway>();
    public DbSet<PathwayLevel> PathwayLevels => Set<PathwayLevel>();
    public DbSet<PathwayLevelCourse> PathwayLevelCourses => Set<PathwayLevelCourse>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CoursePrerequisite> CoursePrerequisites => Set<CoursePrerequisite>();
    public DbSet<CourseSkill> CourseSkills => Set<CourseSkill>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<Assessment> Assessments => Set<Assessment>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<AnswerOption> AnswerOptions => Set<AnswerOption>();
    public DbSet<Project> Projects => Set<Project>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CareersityDbContext).Assembly);
    }
}
