using System.Net;
using Careersity.Domain.Courses;
using Careersity.Domain.Enums;
using Careersity.IntegrationTests.Persistence;
using FluentAssertions;
using Xunit;

namespace Careersity.IntegrationTests.ExternalLearning;

[Collection(PostgreSqlCollection.Name)]
public sealed class ExternalLearningApiTests(PostgreSqlFixture database)
{
    [Fact]
    public async Task CourseEditorLookupQueries_AreTranslatedByPostgreSql()
    {
        var course = new Course("Resource builder", $"resource-builder-{Guid.NewGuid():N}",
            "Course used to verify resource lookup queries.", CourseDifficulty.Foundation, 30);
        await using (var db = database.CreateContext())
        {
            db.Courses.Add(course);
            await db.SaveChangesAsync();
        }

        var authenticated = await TestApiFactory.CreateAdministratorClientAsync(database);
        using var factory = authenticated.Factory;
        using var client = authenticated.Client;

        (await client.GetAsync("/api/admin/instructors?page=1&pageSize=20")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync("/api/admin/external-learning-resources?page=1&pageSize=20")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync($"/api/admin/courses/{course.Id}/external-resources")).StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
