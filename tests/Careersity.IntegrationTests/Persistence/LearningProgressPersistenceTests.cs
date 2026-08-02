using Careersity.Domain.Careers;
using Careersity.Domain.Courses;
using Careersity.Domain.Enums;
using Careersity.Domain.Identity;
using Careersity.Domain.Learning;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Careersity.IntegrationTests.Persistence;

[Collection(PostgreSqlCollection.Name)]
public sealed class LearningProgressPersistenceTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task EnrollmentAggregatePersistsPrivateCollectionsAndReadableStatus()
    {
        var suffix = Guid.NewGuid().ToString("N"); var user = new User($"persist-{suffix}@example.com", "Test", "Learner", new PasswordHasher<User>().HashPassword(null!, "Valid!Pass1"), UserRole.Learner);
        var category = new CareerCategory("Category", $"persist-category-{suffix}"); var career = new Career(category.Id, "Career", $"persist-career-{suffix}", "Description");
        var pathway = new CareerPathway(career.Id, "Path", "1.0"); var course = new Course("Course", $"persist-course-{suffix}", "Description", CourseDifficulty.Beginner, 30);
        var lesson = new Lesson(course.Id, "Lesson", "lesson", LessonContentType.Article, 10, 0); course.AddLesson(lesson);
        var enrollment = new CareerEnrollment(user.Id, career.Id, pathway.Id); var courseProgress = enrollment.AddCourseProgress(course.Id); courseProgress.AddLessonProgress(lesson.Id).Complete();
        await using (var db = fixture.CreateContext()) { db.AddRange(user, category, career, pathway, course, enrollment); await db.SaveChangesAsync(); }
        await using var verify = fixture.CreateContext(); var loaded = await verify.CareerEnrollments.Include(x => x.CourseProgressRecords).ThenInclude(x => x.LessonProgressRecords).SingleAsync(x => x.Id == enrollment.Id);
        loaded.Status.Should().Be(EnrollmentStatus.Active); loaded.CourseProgressRecords.Should().ContainSingle(); loaded.CourseProgressRecords.Single().LessonProgressRecords.Should().ContainSingle(x => x.CompletedAtUtc != null);
        var stored = await verify.Database.SqlQuery<string>($"SELECT \"Status\" AS \"Value\" FROM \"CareerEnrollments\" WHERE \"Id\" = {enrollment.Id}").SingleAsync(); stored.Should().Be("Active");
    }
}
