using System.Data.Common;
using Careersity.Domain.Careers;
using Careersity.Domain.Courses;
using Careersity.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Careersity.Application.Common.Exceptions;
using Careersity.Domain.Skills;
using Careersity.Domain.Assessments;
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

    [Fact]
    public async Task LearningContentRelationshipAndSlugUniqueness_AreEnforced()
    {
        var suffix = Guid.NewGuid().ToString("N");
        await using (var skillContext = fixture.CreateContext())
        {
            skillContext.Skills.AddRange(new Skill("One", $"skill-{suffix}", SkillCategory.Technical),
                new Skill("Two", $"skill-{suffix}", SkillCategory.Tool));
            await skillContext.Invoking(x => x.SaveChangesAsync()).Should().ThrowAsync<ConflictException>();
        }
        var course = CreateCourse($"relationships-{suffix}"); var prerequisite = CreateCourse($"prerequisite-{suffix}");
        var skill = new Skill("Relationship Skill", $"relationship-skill-{suffix}", SkillCategory.Technical);
        await using var context = fixture.CreateContext(); context.AddRange(course, prerequisite, skill); await context.SaveChangesAsync();
        var now = DateTimeOffset.UtcNow;
        await context.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO \"Lessons\" (\"Id\", \"CourseId\", \"Title\", \"Slug\", \"ContentType\", \"EstimatedDurationMinutes\", \"Order\", \"IsRequired\", \"CreatedAtUtc\") VALUES ({Guid.NewGuid()}, {course.Id}, {"One"}, {"same"}, {"Article"}, {10}, {0}, {true}, {now})");
        await FluentActions.Awaiting(() => context.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO \"Lessons\" (\"Id\", \"CourseId\", \"Title\", \"Slug\", \"ContentType\", \"EstimatedDurationMinutes\", \"Order\", \"IsRequired\", \"CreatedAtUtc\") VALUES ({Guid.NewGuid()}, {course.Id}, {"Two"}, {"same"}, {"Article"}, {10}, {1}, {true}, {now})")).Should().ThrowAsync<DbException>();
        await context.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO \"CoursePrerequisites\" (\"Id\", \"CourseId\", \"PrerequisiteCourseId\", \"IsRequired\", \"CreatedAtUtc\") VALUES ({Guid.NewGuid()}, {course.Id}, {prerequisite.Id}, {true}, {now})");
        await FluentActions.Awaiting(() => context.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO \"CoursePrerequisites\" (\"Id\", \"CourseId\", \"PrerequisiteCourseId\", \"IsRequired\", \"CreatedAtUtc\") VALUES ({Guid.NewGuid()}, {course.Id}, {prerequisite.Id}, {false}, {now})")).Should().ThrowAsync<DbException>();
        await context.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO \"CourseSkills\" (\"Id\", \"CourseId\", \"SkillId\", \"ProficiencyLevel\", \"IsPrimary\", \"CreatedAtUtc\") VALUES ({Guid.NewGuid()}, {course.Id}, {skill.Id}, {"Beginner"}, {true}, {now})");
        await FluentActions.Awaiting(() => context.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO \"CourseSkills\" (\"Id\", \"CourseId\", \"SkillId\", \"ProficiencyLevel\", \"IsPrimary\", \"CreatedAtUtc\") VALUES ({Guid.NewGuid()}, {course.Id}, {skill.Id}, {"Advanced"}, {false}, {now})")).Should().ThrowAsync<DbException>();
    }

    [Fact]
    public async Task AssessmentQuestionAndAnswerOptionConstraints_AreEnforced()
    {
        var course = CreateCourse($"activities-{Guid.NewGuid():N}");
        var assessment = new Assessment(course.Id, "Assessment", 70, 3);
        await using var context = fixture.CreateContext(); context.AddRange(course, assessment); await context.SaveChangesAsync();
        var now = DateTimeOffset.UtcNow; var questionId = Guid.NewGuid();
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO \"Questions\" (\"Id\", \"AssessmentId\", \"Prompt\", \"QuestionType\", \"Order\", \"Points\", \"CreatedAtUtc\") VALUES ({questionId}, {assessment.Id}, {"One"}, {"SingleChoice"}, {0}, {1}, {now})");
        await FluentActions.Awaiting(() => context.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO \"Questions\" (\"Id\", \"AssessmentId\", \"Prompt\", \"QuestionType\", \"Order\", \"Points\", \"CreatedAtUtc\") VALUES ({Guid.NewGuid()}, {assessment.Id}, {"Duplicate"}, {"TrueFalse"}, {0}, {1}, {now})")).Should().ThrowAsync<DbException>();
        await FluentActions.Awaiting(() => context.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO \"Questions\" (\"Id\", \"AssessmentId\", \"Prompt\", \"QuestionType\", \"Order\", \"Points\", \"CreatedAtUtc\") VALUES ({Guid.NewGuid()}, {assessment.Id}, {"Invalid"}, {"TrueFalse"}, {1}, {0}, {now})")).Should().ThrowAsync<DbException>();
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO \"AnswerOptions\" (\"Id\", \"QuestionId\", \"Text\", \"IsCorrect\", \"Order\", \"CreatedAtUtc\") VALUES ({Guid.NewGuid()}, {questionId}, {"A"}, {true}, {0}, {now})");
        await FluentActions.Awaiting(() => context.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO \"AnswerOptions\" (\"Id\", \"QuestionId\", \"Text\", \"IsCorrect\", \"Order\", \"CreatedAtUtc\") VALUES ({Guid.NewGuid()}, {questionId}, {"B"}, {false}, {0}, {now})")).Should().ThrowAsync<DbException>();
        await FluentActions.Awaiting(() => context.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO \"Assessments\" (\"Id\", \"CourseId\", \"Title\", \"PassingScorePercentage\", \"MaximumAttempts\", \"Status\", \"CreatedAtUtc\") VALUES ({Guid.NewGuid()}, {course.Id}, {"Invalid attempts"}, {70}, {0}, {"Draft"}, {now})")).Should().ThrowAsync<DbException>();
    }

    private static Course CreateCourse(string slug) => new("Course", slug, "Description", CourseDifficulty.Foundation, 60);
}
