using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Careersity.IntegrationTests.Persistence;

[Collection(PostgreSqlCollection.Name)]
public sealed class MigrationTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task MigrationsApplyAndCreateExpectedTables()
    {
        await using var context = fixture.CreateContext();
        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
        pendingMigrations.Should().BeEmpty();

        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT tablename FROM pg_tables WHERE schemaname = 'public'";
        await using var reader = await command.ExecuteReaderAsync();
        var tables = new List<string>();
        while (await reader.ReadAsync()) tables.Add(reader.GetString(0));

        tables.Should().Contain([
            "CareerCategories", "Careers", "CareerSkills", "CareerPathways", "PathwayLevels",
            "PathwayLevelCourses", "Skills", "Courses", "CoursePrerequisites", "CourseSkills",
            "Lessons", "Assessments", "Questions", "AnswerOptions", "Projects"]);
    }
}
