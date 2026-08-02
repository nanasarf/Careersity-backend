using Careersity.Application.Abstractions.Authentication;
using Careersity.Application.Abstractions.Persistence;
using Careersity.Domain.Enums;
using Careersity.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Careersity.Infrastructure.Authentication;

public sealed class InitialAdministratorInitializer(ICareersityDbContext db, IPasswordService passwords,
    IOptions<InitialAdminOptions> options, ILogger<InitialAdministratorInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (!settings.Enabled) return;
        Validate(settings);
        var normalized = User.NormalizeEmail(settings.Email);
        var existing = await db.Users.SingleOrDefaultAsync(x => x.NormalizedEmail == normalized, cancellationToken);
        if (existing is not null)
        {
            logger.LogInformation("Initial administrator provisioning skipped because the account already exists");
            return;
        }
        var administrator = new User(settings.Email, settings.FirstName, settings.LastName,
            passwords.Hash(settings.Password), UserRole.Administrator);
        db.Users.Add(administrator);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Initial administrator account was created");
    }

    private static void Validate(InitialAdminOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Email) || !System.Net.Mail.MailAddress.TryCreate(options.Email, out _) || string.IsNullOrWhiteSpace(options.FirstName) ||
            string.IsNullOrWhiteSpace(options.LastName) || string.IsNullOrWhiteSpace(options.Password))
            throw new InvalidOperationException("Enabled InitialAdmin provisioning requires email, names, and password.");
        var password = options.Password;
        if (password.Length is < 10 or > 128 || !password.Any(char.IsUpper) || !password.Any(char.IsLower) ||
            !password.Any(char.IsDigit) || !password.Any(x => !char.IsLetterOrDigit(x)))
            throw new InvalidOperationException("InitialAdmin password does not satisfy the configured password policy.");
    }
}
