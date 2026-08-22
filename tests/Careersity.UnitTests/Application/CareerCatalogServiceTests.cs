using Careersity.Application.Abstractions.Persistence;
using Careersity.Application.CareerCatalog.Dtos;
using Careersity.Application.CareerCatalog.Requests;
using Careersity.Application.CareerCatalog.Services;
using Careersity.Application.Common.Exceptions;
using Careersity.Domain.Careers;
using Careersity.Domain.Courses;
using Careersity.Domain.Enums;
using Careersity.Domain.Skills;
using Careersity.Domain.Identity;
using Careersity.Domain.Assessments;
using Careersity.Domain.Projects;
using Careersity.Domain.Learning;
using Careersity.Domain.LearningResources;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace Careersity.UnitTests.Application;

public sealed class CareerCategoryServiceTests
{
    [Fact] public async Task CreateUpdatePublishAndArchive_Work()
    {
        await using var db = TestCatalogContext.Create(); var service = new CareerCategoryService(db);
        var created = await service.CreateAsync(new(" Technology ", "TECH", "Description"), default);
        created.Slug.Should().Be("tech");
        var updated = await service.UpdateAsync(created.Id, new("Business", "business", null), default);
        updated.Name.Should().Be("Business");
        await service.PublishAsync(created.Id, default); (await service.GetAdminAsync(created.Id, default)).Status.Should().Be(ContentStatus.Published);
        await service.ArchiveAsync(created.Id, default); (await service.GetAdminAsync(created.Id, default)).Status.Should().Be(ContentStatus.Archived);
    }

    [Fact] public async Task DuplicateSlugAndMissingCategory_AreRejected()
    {
        await using var db = TestCatalogContext.Create(); var service = new CareerCategoryService(db);
        await service.CreateAsync(new("One", "same", null), default);
        await service.Invoking(x => x.CreateAsync(new("Two", "same", null), default)).Should().ThrowAsync<ConflictException>();
        await service.Invoking(x => x.GetAdminAsync(Guid.NewGuid(), default)).Should().ThrowAsync<NotFoundException>();
    }
}

public sealed class CareerServiceTests
{
    [Fact] public async Task CreateAndPublish_RequireExistingPublishedCategory()
    {
        await using var db = TestCatalogContext.Create(); var service = new CareerService(db);
        await service.Invoking(x => x.CreateAsync(Request(Guid.NewGuid(), "missing"), default)).Should().ThrowAsync<NotFoundException>();
        var category = new CareerCategory("Technology", "technology"); db.Add(category); await db.SaveChangesAsync();
        var career = await service.CreateAsync(Request(category.Id, "career"), default);
        await service.Invoking(x => x.PublishAsync(career.Id, default)).Should().ThrowAsync<ConflictException>();
        category.Publish(); await db.SaveChangesAsync();
        await service.Invoking(x => x.PublishAsync(career.Id, default)).Should().ThrowAsync<ConflictException>();
        var pathway = new CareerPathway(career.Id, "Primary", "1", isPrimary: true); pathway.Publish();
        db.Add(pathway); await db.SaveChangesAsync(); await service.PublishAsync(career.Id, default);
        (await service.GetAdminAsync(career.Id, default)).Status.Should().Be(ContentStatus.Published);
    }

    [Fact] public async Task DuplicateSlugIsRejected_AndPublicSearchExcludesDrafts()
    {
        await using var db = TestCatalogContext.Create(); var category = new CareerCategory("Technology", "technology"); category.Publish(); db.Add(category); await db.SaveChangesAsync();
        var service = new CareerService(db); var published = await service.CreateAsync(Request(category.Id, "published", "Data Engineer"), default);
        var pathway = new CareerPathway(published.Id, "Primary", "1", isPrimary: true); pathway.Publish(); db.Add(pathway); await db.SaveChangesAsync();
        await service.PublishAsync(published.Id, default);
        await service.CreateAsync(Request(category.Id, "draft", "Draft Career"), default);
        await service.Invoking(x => x.CreateAsync(Request(category.Id, "published"), default)).Should().ThrowAsync<ConflictException>();
        var results = await service.ListPublishedAsync(new(1, 20, "data"), default);
        results.Items.Should().ContainSingle(x => x.Id == published.Id); results.Items.Should().NotContain(x => x.Slug == "draft");
    }

    private static CreateCareerRequest Request(Guid categoryId, string slug, string title = "Career") => new(categoryId, title, slug, "Description", null, null, 12);
}

public sealed class CareerSkillServiceTests
{
    [Fact] public async Task AssignUpdateAndRemove_Work()
    {
        await using var db = TestCatalogContext.Create(); var (career, skill) = await SeedAsync(db); var service = new CareerSkillService(db);
        var assignment = await service.AssignAsync(career.Id, new(skill.Id, SkillProficiencyLevel.Beginner, true, 0), default);
        var updated = await service.UpdateAsync(career.Id, assignment.Id, new(SkillProficiencyLevel.Advanced, false, 1), default);
        updated.RequiredProficiencyLevel.Should().Be(SkillProficiencyLevel.Advanced); updated.IsRequired.Should().BeFalse();
        await service.RemoveAsync(career.Id, assignment.Id, default); (await service.ListAsync(career.Id, false, default)).Should().BeEmpty();
    }

    [Fact] public async Task MissingDuplicateSkillAndDisplayOrder_AreRejected()
    {
        await using var db = TestCatalogContext.Create(); var (career, skill) = await SeedAsync(db); var other = new Skill("Other", "other", SkillCategory.Technical); db.Add(other); await db.SaveChangesAsync(); var service = new CareerSkillService(db);
        await service.Invoking(x => x.AssignAsync(career.Id, new(Guid.NewGuid(), SkillProficiencyLevel.Beginner, true, 0), default)).Should().ThrowAsync<NotFoundException>();
        await service.AssignAsync(career.Id, new(skill.Id, SkillProficiencyLevel.Beginner, true, 0), default);
        await service.Invoking(x => x.AssignAsync(career.Id, new(skill.Id, SkillProficiencyLevel.Advanced, true, 1), default)).Should().ThrowAsync<ConflictException>();
        await service.Invoking(x => x.AssignAsync(career.Id, new(other.Id, SkillProficiencyLevel.Advanced, true, 0), default)).Should().ThrowAsync<ConflictException>();
    }

    private static async Task<(Career, Skill)> SeedAsync(TestCatalogContext db)
    { var category = new CareerCategory("Category", "category"); var career = new Career(category.Id, "Career", "career", "Description"); var skill = new Skill("Skill", "skill", SkillCategory.Technical); db.AddRange(category, career, skill); await db.SaveChangesAsync(); return (career, skill); }
}

public sealed class CareerPathwayServiceTests
{
    [Fact] public async Task NewPrimaryDemotesExisting_AndDuplicateVersionIsRejected()
    {
        await using var db = TestCatalogContext.Create(); var career = await SeedCareerAsync(db); var service = new CareerPathwayService(db);
        var first = await service.CreateAsync(career.Id, new(career.Id, "First", null, "1", true), default);
        await service.CreateAsync(career.Id, new(career.Id, "Second", null, "2", true), default);
        (await service.GetAdminAsync(career.Id, first.Id, default)).IsPrimary.Should().BeFalse();
        await service.Invoking(x => x.CreateAsync(career.Id, new(career.Id, "Duplicate", null, "2", false), default)).Should().ThrowAsync<ConflictException>();
    }

    [Fact] public async Task LevelsCanBeAddedAndReorderedAtomically()
    {
        await using var db = TestCatalogContext.Create(); var career = await SeedCareerAsync(db); var service = new CareerPathwayService(db);
        var pathway = await service.CreateAsync(career.Id, new(career.Id, "Path", null, "1", true), default);
        var first = await service.AddLevelAsync(career.Id, pathway.Id, new("One", null, 0), default);
        var second = await service.AddLevelAsync(career.Id, pathway.Id, new("Two", null, 1), default);
        await service.Invoking(x => x.AddLevelAsync(career.Id, pathway.Id, new("Duplicate", null, 1), default)).Should().ThrowAsync<ConflictException>();
        await service.ReorderLevelsAsync(career.Id, pathway.Id, new([new(first.Id, 1), new(second.Id, 0)]), default);
        (await service.GetAdminAsync(career.Id, pathway.Id, default)).Levels.OrderBy(x => x.Order).Select(x => x.Id).Should().Equal(second.Id, first.Id);
    }

    [Fact] public async Task PublicationRulesAndPublishedImmutability_AreEnforced()
    {
        await using var db = TestCatalogContext.Create(); var career = await SeedCareerAsync(db); var service = new CareerPathwayService(db);
        var pathway = await service.CreateAsync(career.Id, new(career.Id, "Path", null, "1", true), default);
        await service.Invoking(x => x.PublishAsync(career.Id, pathway.Id, default)).Should().ThrowAsync<ConflictException>();
        var level = await service.AddLevelAsync(career.Id, pathway.Id, new("Level", null, 0), default);
        await service.Invoking(x => x.PublishAsync(career.Id, pathway.Id, default)).Should().ThrowAsync<ConflictException>();
        var course = new Course("Course", "course", "Description", CourseDifficulty.Foundation, 60); db.Add(course); await db.SaveChangesAsync();
        await service.AddCourseAsync(career.Id, pathway.Id, level.Id, new(course.Id, 0, true), default);
        await service.Invoking(x => x.PublishAsync(career.Id, pathway.Id, default)).Should().ThrowAsync<ConflictException>();
        var lesson = new Lesson(course.Id, "Lesson", "lesson", LessonContentType.Article, 5, 0);
        course.AddLesson(lesson); db.Add(lesson); course.Publish(); await db.SaveChangesAsync();
        await service.PublishAsync(career.Id, pathway.Id, default);
        await service.Invoking(x => x.AddLevelAsync(career.Id, pathway.Id, new("Late", null, 1), default)).Should().ThrowAsync<ConflictException>();
    }

    private static async Task<Career> SeedCareerAsync(TestCatalogContext db, bool published = false)
    { var category = new CareerCategory("Category", $"category-{Guid.NewGuid():N}"); var career = new Career(category.Id, "Career", $"career-{Guid.NewGuid():N}", "Description"); if (published) { category.Publish(); career.Publish(); } db.AddRange(category, career); await db.SaveChangesAsync(); return career; }
}

internal sealed class TestCatalogContext : DbContext, ICareersityDbContext
{
    private TestCatalogContext(DbContextOptions<TestCatalogContext> options) : base(options) { }
    internal static TestCatalogContext Create() => new(new DbContextOptionsBuilder<TestCatalogContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);
    public DbSet<CareerCategory> CareerCategories => Set<CareerCategory>(); public DbSet<Career> Careers => Set<Career>();
    public DbSet<CareerSkill> CareerSkills => Set<CareerSkill>(); public DbSet<CareerPathway> CareerPathways => Set<CareerPathway>();
    public DbSet<PathwayLevel> PathwayLevels => Set<PathwayLevel>(); public DbSet<PathwayLevelCourse> PathwayLevelCourses => Set<PathwayLevelCourse>();
    public DbSet<Skill> Skills => Set<Skill>(); public DbSet<Course> Courses => Set<Course>();
    public DbSet<Lesson> Lessons => Set<Lesson>(); public DbSet<CoursePrerequisite> CoursePrerequisites => Set<CoursePrerequisite>();
    public DbSet<CourseSkill> CourseSkills => Set<CourseSkill>();
    public DbSet<Assessment> Assessments => Set<Assessment>(); public DbSet<Question> Questions => Set<Question>();
    public DbSet<AnswerOption> AnswerOptions => Set<AnswerOption>(); public DbSet<Project> Projects => Set<Project>();
    public DbSet<User> Users => Set<User>(); public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<CareerEnrollment> CareerEnrollments => Set<CareerEnrollment>(); public DbSet<CourseProgress> CourseProgressRecords => Set<CourseProgress>();
    public DbSet<LessonProgress> LessonProgressRecords => Set<LessonProgress>();
    public DbSet<AssessmentAttempt> AssessmentAttempts => Set<AssessmentAttempt>();
    public DbSet<AssessmentResponse> AssessmentResponses => Set<AssessmentResponse>();
    public DbSet<AssessmentResponseOption> AssessmentResponseOptions => Set<AssessmentResponseOption>();
    public DbSet<LearningProvider> LearningProviders => Set<LearningProvider>();
    public DbSet<Instructor> Instructors => Set<Instructor>();
    public DbSet<ExternalLearningResource> ExternalLearningResources => Set<ExternalLearningResource>();
    public DbSet<CourseExternalResource> CourseExternalResources => Set<CourseExternalResource>();
    public DbSet<ExternalResourceProgress> ExternalResourceProgressRecords => Set<ExternalResourceProgress>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<CareerPathway>().HasMany(x => x.Levels).WithOne().HasForeignKey(x => x.CareerPathwayId);
        builder.Entity<PathwayLevel>().HasMany(x => x.Courses).WithOne().HasForeignKey(x => x.PathwayLevelId);
        builder.Entity<User>().HasMany(x => x.RefreshTokens).WithOne().HasForeignKey(x => x.UserId);
        builder.Entity<Course>().HasMany(x => x.Lessons).WithOne().HasForeignKey(x => x.CourseId);
        builder.Entity<Course>().HasMany(x => x.Prerequisites).WithOne().HasForeignKey(x => x.CourseId);
        builder.Entity<Course>().HasMany(x => x.CourseSkills).WithOne().HasForeignKey(x => x.CourseId);
        builder.Entity<Assessment>().HasMany(x => x.Questions).WithOne().HasForeignKey(x => x.AssessmentId);
        builder.Entity<Question>().HasMany(x => x.AnswerOptions).WithOne().HasForeignKey(x => x.QuestionId);
        builder.Entity<CareerEnrollment>().HasMany(x => x.CourseProgressRecords).WithOne().HasForeignKey(x => x.CareerEnrollmentId);
        builder.Entity<CourseProgress>().HasMany(x => x.LessonProgressRecords).WithOne().HasForeignKey(x => x.CourseProgressId);
        builder.Entity<AssessmentAttempt>().HasMany(x => x.Responses).WithOne().HasForeignKey(x => x.AssessmentAttemptId);
        builder.Entity<AssessmentResponse>().HasMany(x => x.SelectedOptions).WithOne().HasForeignKey(x => x.AssessmentResponseId);
        builder.Entity<CourseProgress>().HasMany(x => x.ExternalResourceProgressRecords).WithOne().HasForeignKey(x => x.CourseProgressId);
    }
}
