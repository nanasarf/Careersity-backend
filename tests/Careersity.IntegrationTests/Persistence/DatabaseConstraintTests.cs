using System.Data.Common;
using Careersity.Domain.Careers;
using Careersity.Domain.Courses;
using Careersity.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Careersity.Application.Common.Exceptions;
using Xunit;

namespace Careersity.IntegrationTests.Persistence;

[Collection(PostgreSqlCollection.Name)]
public sealed class DatabaseConstraintTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task UniqueSlugs_AreEnforced()
    {
        var suffix = Guid.NewGuid().ToString("N");
        await using var context = fixture.CreateContext();
        context.CareerCategories.AddRange(new CareerCategory("One", $"duplicate-{suffix}"), new CareerCategory("Two", $"duplicate-{suffix}"));
        await context.Invoking(x => x.SaveChangesAsync()).Should().ThrowAsync<ConflictException>();

        await using var courseContext = fixture.CreateContext();
        courseContext.Courses.AddRange(CreateCourse($"duplicate-course-{suffix}"), CreateCourse($"duplicate-course-{suffix}"));
        await courseContext.Invoking(x => x.SaveChangesAsync()).Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task AggregateOrderingConstraints_AreEnforced()
    {
        var category = new CareerCategory("Category", $"category-{Guid.NewGuid():N}");
        var career = new Career(category.Id, "Career", $"career-{Guid.NewGuid():N}", "Description");
        var pathway = new CareerPathway(career.Id, "Pathway", "1.0");
        var course = CreateCourse($"course-{Guid.NewGuid():N}");
        await using var context = fixture.CreateContext();
        context.AddRange(category, career, pathway, course); await context.SaveChangesAsync();

        var now = DateTimeOffset.UtcNow;
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO \"PathwayLevels\" (\"Id\", \"CareerPathwayId\", \"Name\", \"Order\", \"CreatedAtUtc\") VALUES ({Guid.NewGuid()}, {pathway.Id}, {"A"}, {0}, {now})");
        var duplicateLevel = () => context.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO \"PathwayLevels\" (\"Id\", \"CareerPathwayId\", \"Name\", \"Order\", \"CreatedAtUtc\") VALUES ({Guid.NewGuid()}, {pathway.Id}, {"B"}, {0}, {now})");
        await duplicateLevel.Should().ThrowAsync<DbException>();

        await context.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO \"Lessons\" (\"Id\", \"CourseId\", \"Title\", \"Slug\", \"ContentType\", \"EstimatedDurationMinutes\", \"Order\", \"IsRequired\", \"CreatedAtUtc\") VALUES ({Guid.NewGuid()}, {course.Id}, {"A"}, {"a"}, {"Article"}, {10}, {0}, {true}, {now})");
        var duplicateLesson = () => context.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO \"Lessons\" (\"Id\", \"CourseId\", \"Title\", \"Slug\", \"ContentType\", \"EstimatedDurationMinutes\", \"Order\", \"IsRequired\", \"CreatedAtUtc\") VALUES ({Guid.NewGuid()}, {course.Id}, {"B"}, {"b"}, {"Article"}, {10}, {0}, {true}, {now})");
        await duplicateLesson.Should().ThrowAsync<DbException>();
    }

    [Fact]
    public async Task CheckConstraints_RejectInvalidValuesInsertedOutsideDomain()
    {
        var course = CreateCourse($"constraint-course-{Guid.NewGuid():N}");
        await using var context = fixture.CreateContext(); context.Add(course); await context.SaveChangesAsync();
        var now = DateTimeOffset.UtcNow;

        var selfReference = () => context.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO \"CoursePrerequisites\" (\"Id\", \"CourseId\", \"PrerequisiteCourseId\", \"IsRequired\", \"CreatedAtUtc\") VALUES ({Guid.NewGuid()}, {course.Id}, {course.Id}, {true}, {now})");
        await selfReference.Should().ThrowAsync<DbException>();

        var invalidAssessment = () => context.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO \"Assessments\" (\"Id\", \"CourseId\", \"Title\", \"PassingScorePercentage\", \"Status\", \"CreatedAtUtc\") VALUES ({Guid.NewGuid()}, {course.Id}, {"Invalid"}, {101}, {"Draft"}, {now})");
        await invalidAssessment.Should().ThrowAsync<DbException>();

        var invalidCourse = () => context.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO \"Courses\" (\"Id\", \"Title\", \"Slug\", \"ShortDescription\", \"Difficulty\", \"EstimatedDurationMinutes\", \"Status\", \"CreatedAtUtc\") VALUES ({Guid.NewGuid()}, {"Invalid"}, {$"invalid-{Guid.NewGuid():N}"}, {"Invalid"}, {"Foundation"}, {0}, {"Draft"}, {now})");
        await invalidCourse.Should().ThrowAsync<DbException>();
    }

    private static Course CreateCourse(string slug) => new("Course", slug, "Description", CourseDifficulty.Foundation, 60);
}
