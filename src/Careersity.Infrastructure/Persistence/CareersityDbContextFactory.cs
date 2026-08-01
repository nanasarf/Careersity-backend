using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Careersity.Infrastructure.Persistence;

/// <summary>Creates the database context for EF Core command-line tooling.</summary>
public sealed class CareersityDbContextFactory : IDesignTimeDbContextFactory<CareersityDbContext>
{
    public CareersityDbContext CreateDbContext(string[] args)
    {
        var apiDirectory = FindApiDirectory();
        var connectionString = ReadConnectionString(Path.Combine(apiDirectory, "appsettings.json"));
        connectionString = ReadConnectionString(Path.Combine(apiDirectory, "appsettings.Development.json")) ?? connectionString;
        connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__CareersityDatabase") ?? connectionString;

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Set ConnectionStrings__CareersityDatabase or configure ConnectionStrings:CareersityDatabase before using EF tools.");
        }

        var options = new DbContextOptionsBuilder<CareersityDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new CareersityDbContext(options);
    }

    private static string FindApiDirectory()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            var nested = Path.Combine(current.FullName, "src", "Careersity.Api");
            if (Directory.Exists(nested)) return nested;
            var sibling = Path.Combine(current.FullName, "..", "Careersity.Api");
            if (Directory.Exists(sibling)) return Path.GetFullPath(sibling);
            current = current.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate the Careersity.Api configuration directory.");
    }

    private static string? ReadConnectionString(string path)
    {
        if (!File.Exists(path)) return null;
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.TryGetProperty("ConnectionStrings", out var section)
            && section.TryGetProperty("CareersityDatabase", out var value)
                ? value.GetString()
                : null;
    }
}
