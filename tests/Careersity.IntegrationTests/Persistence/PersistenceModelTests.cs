using Careersity.Domain.Assessments;
using Careersity.Domain.Careers;
using Careersity.Domain.Courses;
using Careersity.Domain.Enums;
using Careersity.Domain.Projects;
using Careersity.Domain.Skills;
using Careersity.Infrastructure.Persistence;
using Careersity.Domain.Identity;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Careersity.IntegrationTests.Persistence;

public sealed class PersistenceModelTests
{
    [Fact]
    public void Model_MapsEveryEntityWithKeysStringEnumsAndExplicitForeignKeys()
    {
        var options = new DbContextOptionsBuilder<CareersityDbContext>()
            .UseNpgsql("Host=localhost;Database=model_only;Username=postgres;Password=postgres")
            .Options;
        using var context = new CareersityDbContext(options);
        var expectedTypes = new[]
        {
            typeof(CareerCategory), typeof(Career), typeof(CareerSkill), typeof(CareerPathway),
            typeof(PathwayLevel), typeof(PathwayLevelCourse), typeof(Skill), typeof(Course),
            typeof(CoursePrerequisite), typeof(CourseSkill), typeof(Lesson), typeof(Assessment),
            typeof(Question), typeof(AnswerOption), typeof(Project), typeof(User), typeof(RefreshToken)
            , typeof(Careersity.Domain.Learning.AssessmentAttempt), typeof(Careersity.Domain.Learning.AssessmentResponse), typeof(Careersity.Domain.Learning.AssessmentResponseOption)
        };

        foreach (var type in expectedTypes)
        {
            var entity = context.Model.FindEntityType(type);
            entity.Should().NotBeNull($"{type.Name} should be mapped");
            entity!.FindPrimaryKey().Should().NotBeNull();
        }

        context.Model.GetEntityTypes().SelectMany(x => x.GetForeignKeys()).SelectMany(x => x.Properties)
            .Should().NotBeEmpty().And.OnlyContain(x => !x.IsShadowProperty());

        var enumProperties = context.Model.GetEntityTypes().SelectMany(x => x.GetProperties())
            .Where(x => x.ClrType.IsEnum);
        enumProperties.Should().NotBeEmpty();
        enumProperties.Select(x => x.GetTypeMapping().Converter?.ProviderClrType)
            .Should().OnlyContain(x => x == typeof(string));
    }
}
