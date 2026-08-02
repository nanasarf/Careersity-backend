using Careersity.Application.Common.Exceptions;
using Careersity.Application.LearningContent.Requests;
using Careersity.Application.LearningContent.Services;
using Careersity.Domain.Courses;
using Careersity.Domain.Enums;
using Careersity.Domain.Skills;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Careersity.UnitTests.Application;

public sealed class LearningContentServiceTests
{
    [Fact]
    public async Task SkillLifecycle_CreateUpdatePublishArchiveAndPublicVisibility()
    {
        await using var db = TestCatalogContext.Create(); var service = new SkillService(db);
        var skill = await service.CreateAsync(new(" C# ", " CSharp ", "Language", SkillCategory.Technical), default);
        skill.Status.Should().Be(ContentStatus.Draft); skill.Slug.Should().Be("csharp");
        (await service.ListPublishedAsync(new(), default)).Items.Should().BeEmpty();
        skill = await service.UpdateAsync(skill.Id, new("C# Programming", "csharp", "Updated", SkillCategory.Technical), default);
        await service.PublishAsync(skill.Id, default);
        (await service.ListPublishedAsync(new(Search: "program", Category: SkillCategory.Technical), default)).Items.Should().ContainSingle();
        var update = () => service.UpdateAsync(skill.Id, new("No", "no", null, SkillCategory.Tool), default);
        await update.Should().ThrowAsync<ConflictException>();
        await service.ArchiveAsync(skill.Id, default);
        (await service.ListPublishedAsync(new(), default)).Items.Should().BeEmpty();
    }

    [Fact]
    public async Task SkillAndCourseDuplicateSlugsAreRejected()
    {
        await using var db = TestCatalogContext.Create(); var skills = new SkillService(db); var courses = new CourseService(db);
        await skills.CreateAsync(new("One", "duplicate", null, SkillCategory.Technical), default);
        await FluentActions.Awaiting(() => skills.CreateAsync(new("Two", "DUPLICATE", null, SkillCategory.Tool), default)).Should().ThrowAsync<ConflictException>();
        await courses.CreateAsync(CourseRequest("one", "course"), default);
        await FluentActions.Awaiting(() => courses.CreateAsync(CourseRequest("two", "COURSE"), default)).Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task LessonCrudRejectsDuplicatesSupportsAtomicReorderAndExternalValidation()
    {
        await using var db = TestCatalogContext.Create(); var course = await AddCourseAsync(db); var service = new LessonService(db);
        var first = await service.AddAsync(course.Id, LessonRequest("First", "first", 0), default);
        var second = await service.AddAsync(course.Id, LessonRequest("Second", "second", 1), default);
        await FluentActions.Awaiting(() => service.AddAsync(course.Id, LessonRequest("Duplicate", "third", 1), default)).Should().ThrowAsync<ConflictException>();
        await FluentActions.Awaiting(() => service.AddAsync(course.Id, LessonRequest("Duplicate", "FIRST", 2), default)).Should().ThrowAsync<ConflictException>();
        await FluentActions.Awaiting(() => service.AddAsync(course.Id, LessonRequest("External", "external", 2, LessonContentType.ExternalResource, "relative"), default)).Should().ThrowAsync<ArgumentException>();
        await service.ReorderAsync(course.Id, new([new(first.Id, 1), new(second.Id, 0)]), default);
        (await db.Lessons.OrderBy(x => x.Order).Select(x => x.Id).ToListAsync()).Should().Equal(second.Id, first.Id);
        await FluentActions.Awaiting(() => service.ReorderAsync(course.Id, new([new(first.Id, 0)]), default)).Should().ThrowAsync<ConflictException>();
        await service.UpdateAsync(course.Id, first.Id, new("Updated", "updated", null, "Body", LessonContentType.Article, null, 20, 1, false), default);
        await service.RemoveAsync(course.Id, second.Id, default); (await db.Lessons.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task PrerequisitesRejectMissingSelfDuplicateDirectAndIndirectCycles()
    {
        await using var db = TestCatalogContext.Create(); var a = await AddCourseAsync(db, "a"); var b = await AddCourseAsync(db, "b"); var c = await AddCourseAsync(db, "c");
        var service = new CoursePrerequisiteService(db);
        await FluentActions.Awaiting(() => service.AddAsync(a.Id, new(Guid.NewGuid(), true), default)).Should().ThrowAsync<NotFoundException>();
        await FluentActions.Awaiting(() => service.AddAsync(a.Id, new(a.Id, true), default)).Should().ThrowAsync<ConflictException>();
        await service.AddAsync(a.Id, new(b.Id, true), default);
        await FluentActions.Awaiting(() => service.AddAsync(a.Id, new(b.Id, true), default)).Should().ThrowAsync<ConflictException>();
        await FluentActions.Awaiting(() => service.AddAsync(b.Id, new(a.Id, true), default)).Should().ThrowAsync<ConflictException>();
        var bc = await service.AddAsync(b.Id, new(c.Id, false), default);
        await FluentActions.Awaiting(() => service.AddAsync(c.Id, new(a.Id, true), default)).Should().ThrowAsync<ConflictException>();
        var updated = await service.UpdateAsync(b.Id, bc.Id, new(true), default); updated.IsRequired.Should().BeTrue();
        await service.RemoveAsync(b.Id, bc.Id, default);
    }

    [Fact]
    public async Task CourseSkillsSupportCrudAndRejectMissingDuplicateAndPublishedMutation()
    {
        await using var db = TestCatalogContext.Create(); var course = await AddCourseAsync(db); var service = new CourseSkillService(db);
        await FluentActions.Awaiting(() => service.AddAsync(course.Id, new(Guid.NewGuid(), SkillProficiencyLevel.Beginner, true), default)).Should().ThrowAsync<NotFoundException>();
        var skill = new Skill("C#", Guid.NewGuid().ToString("N"), SkillCategory.Technical); db.Skills.Add(skill); await db.SaveChangesAsync();
        var relation = await service.AddAsync(course.Id, new(skill.Id, SkillProficiencyLevel.Beginner, false), default);
        await FluentActions.Awaiting(() => service.AddAsync(course.Id, new(skill.Id, SkillProficiencyLevel.Advanced, true), default)).Should().ThrowAsync<ConflictException>();
        relation = await service.UpdateAsync(course.Id, relation.Id, new(SkillProficiencyLevel.Intermediate, true), default); relation.IsPrimary.Should().BeTrue();
        await service.RemoveAsync(course.Id, relation.Id, default); (await db.CourseSkills.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task CoursePublicationEnforcesCrossAggregateRulesAndImmutability()
    {
        await using var db = TestCatalogContext.Create(); var course = await AddCourseAsync(db); var courses = new CourseService(db);
        await FluentActions.Awaiting(() => courses.PublishAsync(course.Id, default)).Should().ThrowAsync<ConflictException>();
        var lessons = new LessonService(db); await lessons.AddAsync(course.Id, LessonRequest("Optional", "optional", 0) with { IsRequired = false }, default);
        await FluentActions.Awaiting(() => courses.PublishAsync(course.Id, default)).Should().ThrowAsync<ConflictException>();
        await lessons.UpdateAsync(course.Id, (await db.Lessons.SingleAsync()).Id, new("Required", "required", null, null, LessonContentType.Article, null, 10, 0, true), default);
        await FluentActions.Awaiting(() => courses.PublishAsync(course.Id, default)).Should().ThrowAsync<ConflictException>();
        var skill = new Skill("Skill", Guid.NewGuid().ToString("N"), SkillCategory.Technical); db.Skills.Add(skill); await db.SaveChangesAsync();
        var courseSkills = new CourseSkillService(db); await courseSkills.AddAsync(course.Id, new(skill.Id, SkillProficiencyLevel.Beginner, false), default);
        await FluentActions.Awaiting(() => courses.PublishAsync(course.Id, default)).Should().ThrowAsync<ConflictException>();
        var relation = await db.CourseSkills.SingleAsync(); await courseSkills.UpdateAsync(course.Id, relation.Id, new(SkillProficiencyLevel.Beginner, true), default);
        await FluentActions.Awaiting(() => courses.PublishAsync(course.Id, default)).Should().ThrowAsync<ConflictException>();
        skill.Publish(); await db.SaveChangesAsync(); await courses.PublishAsync(course.Id, default);
        (await db.Courses.FindAsync(course.Id))!.Status.Should().Be(ContentStatus.Published);
        await FluentActions.Awaiting(() => courses.UpdateAsync(course.Id, new UpdateCourseRequest("changed", "changed", "Description", null, CourseDifficulty.Beginner, 60), default)).Should().ThrowAsync<ConflictException>();
        await FluentActions.Awaiting(() => lessons.AddAsync(course.Id, LessonRequest("No", "no", 1), default)).Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task CoursePublicQueriesFilterSearchDifficultyAndSkill()
    {
        await using var db = TestCatalogContext.Create(); var skill = new Skill("SQL", "sql", SkillCategory.Technical); skill.Publish(); db.Skills.Add(skill);
        var published = new Course("SQL Basics", "sql-basics", "Learn SQL", CourseDifficulty.Beginner, 30);
        published.AddLesson(new(published.Id, "Intro", "intro", LessonContentType.Article, 10, 0)); published.AssociateSkill(skill.Id, SkillProficiencyLevel.Beginner, true); published.Publish();
        var draft = new Course("Hidden", "hidden", "Draft course", CourseDifficulty.Advanced, 30); db.Courses.AddRange(published, draft); await db.SaveChangesAsync();
        var results = await new CourseService(db).ListPublishedAsync(new(Search: "sql", Difficulty: CourseDifficulty.Beginner, SkillId: skill.Id), default);
        results.Items.Should().ContainSingle(x => x.Id == published.Id); results.Items.Should().NotContain(x => x.Id == draft.Id);
    }

    private static CreateCourseRequest CourseRequest(string title, string slug) => new(title, slug, "Description", null, CourseDifficulty.Beginner, 60);
    private static AddLessonRequest LessonRequest(string title, string slug, int order, LessonContentType type = LessonContentType.Article, string? url = null) =>
        new(title, slug, null, null, type, url, 10, order, true);
    private static async Task<Course> AddCourseAsync(TestCatalogContext db, string? slug = null)
    { var course = new Course(slug ?? "Course", slug ?? Guid.NewGuid().ToString("N"), "Description", CourseDifficulty.Beginner, 60); db.Courses.Add(course); await db.SaveChangesAsync(); return course; }
}
