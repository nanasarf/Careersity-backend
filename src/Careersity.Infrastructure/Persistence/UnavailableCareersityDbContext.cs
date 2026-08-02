using Careersity.Application.Abstractions.Persistence;
using Careersity.Application.Common.Exceptions;
using Careersity.Domain.Careers;
using Careersity.Domain.Courses;
using Careersity.Domain.Skills;
using Careersity.Domain.Identity;
using Careersity.Domain.Assessments;
using Careersity.Domain.Projects;
using Careersity.Domain.Learning;
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
    public DbSet<Lesson> Lessons => throw Error();
    public DbSet<CoursePrerequisite> CoursePrerequisites => throw Error();
    public DbSet<CourseSkill> CourseSkills => throw Error();
    public DbSet<Assessment> Assessments => throw Error();
    public DbSet<Question> Questions => throw Error();
    public DbSet<AnswerOption> AnswerOptions => throw Error();
    public DbSet<Project> Projects => throw Error();
    public DbSet<User> Users => throw Error();
    public DbSet<RefreshToken> RefreshTokens => throw Error();
    public DbSet<CareerEnrollment> CareerEnrollments => throw Error();
    public DbSet<CourseProgress> CourseProgressRecords => throw Error();
    public DbSet<LessonProgress> LessonProgressRecords => throw Error();
    public DbSet<AssessmentAttempt> AssessmentAttempts => throw Error();
    public DbSet<AssessmentResponse> AssessmentResponses => throw Error();
    public DbSet<AssessmentResponseOption> AssessmentResponseOptions => throw Error();
    public DatabaseFacade Database => throw Error();
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => throw Error();
}
