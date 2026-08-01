using Careersity.Domain.Assessments;
using Careersity.Domain.Careers;
using Careersity.Domain.Courses;
using Careersity.Domain.Enums;
using Careersity.Domain.Projects;
using Careersity.Domain.Skills;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Careersity.IntegrationTests.Persistence;

[Collection(PostgreSqlCollection.Name)]
public sealed class AggregatePersistenceTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task CareerAndCategory_RoundTrip()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var category = new CareerCategory("Technology", $"technology-{suffix}", "Technology careers");
        var career = new Career(category.Id, "Software Engineer", $"software-engineer-{suffix}", "Builds software.", estimatedDurationWeeks: 24);
        await using (var context = fixture.CreateContext())
        {
            context.AddRange(category, career); await context.SaveChangesAsync();
        }

        await using var verification = fixture.CreateContext();
        var stored = await verification.Careers.SingleAsync(x => x.Id == career.Id);
        stored.CareerCategoryId.Should().Be(category.Id);
        stored.Title.Should().Be("Software Engineer");
        (await verification.CareerCategories.FindAsync(category.Id))!.Description.Should().Be("Technology careers");
    }

    [Fact]
    public async Task PathwayAggregate_RoundTripsPrivateCollections()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var category = new CareerCategory("Data", $"data-{suffix}");
        var career = new Career(category.Id, "Data Analyst", $"data-analyst-{suffix}", "Analyzes data.");
        var firstCourse = CreateCourse($"sql-{suffix}");
        var secondCourse = CreateCourse($"visualization-{suffix}");
        var pathway = new CareerPathway(career.Id, "Core", "1.0", isPrimary: true);
        var firstLevel = new PathwayLevel(pathway.Id, "Foundation", 0);
        firstLevel.AddCourse(firstCourse.Id, 0, true);
        var secondLevel = new PathwayLevel(pathway.Id, "Applied", 1);
        secondLevel.AddCourse(secondCourse.Id, 0, false);
        pathway.AddLevel(firstLevel); pathway.AddLevel(secondLevel);

        await using (var context = fixture.CreateContext())
        {
            context.AddRange(category, career, firstCourse, secondCourse, pathway); await context.SaveChangesAsync();
        }
        await using var verification = fixture.CreateContext();
        var stored = await verification.CareerPathways.Include(x => x.Levels).ThenInclude(x => x.Courses)
            .SingleAsync(x => x.Id == pathway.Id);
        stored.Levels.OrderBy(x => x.Order).Select(x => x.Name).Should().Equal("Foundation", "Applied");
        stored.Levels.Single(x => x.Order == 0).Courses.Single().IsRequired.Should().BeTrue();
        stored.Levels.Single(x => x.Order == 1).Courses.Single().IsRequired.Should().BeFalse();
    }

    [Fact]
    public async Task CourseAggregate_RoundTripsPrivateCollections()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var prerequisite = CreateCourse($"foundation-{suffix}");
        var course = CreateCourse($"advanced-{suffix}");
        course.AddLesson(new Lesson(course.Id, "One", "one", LessonContentType.Article, 10, 0));
        course.AddLesson(new Lesson(course.Id, "Two", "two", LessonContentType.Video, 20, 1));
        var skill = new Skill("Programming", $"programming-{suffix}", SkillCategory.Technical);
        course.AssociateSkill(skill.Id, SkillProficiencyLevel.Intermediate, true);
        course.AddPrerequisite(prerequisite.Id);

        await using (var context = fixture.CreateContext())
        {
            context.AddRange(prerequisite, skill, course); await context.SaveChangesAsync();
        }
        await using var verification = fixture.CreateContext();
        var stored = await verification.Courses.Include(x => x.Lessons).Include(x => x.CourseSkills)
            .Include(x => x.Prerequisites).SingleAsync(x => x.Id == course.Id);
        stored.Lessons.OrderBy(x => x.Order).Select(x => x.Title).Should().Equal("One", "Two");
        stored.CourseSkills.Single().SkillId.Should().Be(skill.Id);
        stored.Prerequisites.Single().PrerequisiteCourseId.Should().Be(prerequisite.Id);
    }

    [Fact]
    public async Task AssessmentAggregate_RoundTripsNestedPrivateCollections()
    {
        var course = CreateCourse($"assessment-course-{Guid.NewGuid():N}");
        var assessment = new Assessment(course.Id, "Knowledge check", 80, 2);
        var question = new Question(assessment.Id, "Choose one", QuestionType.SingleChoice, 0, 5);
        question.AddAnswerOption(new AnswerOption(question.Id, "Correct", true, 0));
        question.AddAnswerOption(new AnswerOption(question.Id, "Incorrect", false, 1));
        assessment.AddQuestion(question);
        await using (var context = fixture.CreateContext())
        {
            context.AddRange(course, assessment); await context.SaveChangesAsync();
        }
        await using var verification = fixture.CreateContext();
        var stored = await verification.Assessments.Include(x => x.Questions).ThenInclude(x => x.AnswerOptions)
            .SingleAsync(x => x.Id == assessment.Id);
        stored.Questions.Should().ContainSingle();
        stored.Questions.Single().AnswerOptions.Should().HaveCount(2);
        stored.Questions.Single().IsValid().Should().BeTrue();
    }

    [Fact]
    public async Task ProjectAndEnums_RoundTripAsReadableValues()
    {
        var course = CreateCourse($"project-course-{Guid.NewGuid():N}");
        var project = new Project(course.Id, "Portfolio", "Build a portfolio.", new string('I', 2_500),
            ProjectSubmissionType.RepositoryUrl, 120, "Working repository", new string('E', 2_500));
        await using (var context = fixture.CreateContext())
        {
            context.AddRange(course, project); await context.SaveChangesAsync();
        }
        await using var verification = fixture.CreateContext();
        var stored = await verification.Projects.SingleAsync(x => x.Id == project.Id);
        stored.Instructions.Should().HaveLength(2_500);
        stored.EvaluationCriteria.Should().HaveLength(2_500);
        stored.SubmissionType.Should().Be(ProjectSubmissionType.RepositoryUrl);

        var connection = verification.Database.GetDbConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT \"SubmissionType\" FROM \"Projects\" WHERE \"Id\" = @id";
        var parameter = command.CreateParameter(); parameter.ParameterName = "id"; parameter.Value = project.Id; command.Parameters.Add(parameter);
        (await command.ExecuteScalarAsync()).Should().Be("RepositoryUrl");
    }

    private static Course CreateCourse(string slug) =>
        new("Course", slug, "Course description.", CourseDifficulty.Foundation, 60);
}
