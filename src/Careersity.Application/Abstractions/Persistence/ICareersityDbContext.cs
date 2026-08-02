using Careersity.Domain.Careers;
using Careersity.Domain.Courses;
using Careersity.Domain.Skills;
using Careersity.Domain.Identity;
using Careersity.Domain.Assessments;
using Careersity.Domain.Projects;
using Careersity.Domain.Learning;
using Careersity.Domain.LearningResources;
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
    DbSet<Lesson> Lessons { get; }
    DbSet<CoursePrerequisite> CoursePrerequisites { get; }
    DbSet<CourseSkill> CourseSkills { get; }
    DbSet<Assessment> Assessments { get; }
    DbSet<Question> Questions { get; }
    DbSet<AnswerOption> AnswerOptions { get; }
    DbSet<Project> Projects { get; }
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<CareerEnrollment> CareerEnrollments { get; }
    DbSet<CourseProgress> CourseProgressRecords { get; }
    DbSet<LessonProgress> LessonProgressRecords { get; }
    DbSet<AssessmentAttempt> AssessmentAttempts { get; }
    DbSet<AssessmentResponse> AssessmentResponses { get; }
    DbSet<AssessmentResponseOption> AssessmentResponseOptions { get; }
    DbSet<LearningProvider> LearningProviders { get; }
    DbSet<Instructor> Instructors { get; }
    DbSet<ExternalLearningResource> ExternalLearningResources { get; }
    DbSet<CourseExternalResource> CourseExternalResources { get; }
    DbSet<ExternalResourceProgress> ExternalResourceProgressRecords { get; }
    DatabaseFacade Database { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
