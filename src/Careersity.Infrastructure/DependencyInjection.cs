using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Careersity.Infrastructure.Persistence;
using Careersity.Application.Abstractions.Persistence;
using Careersity.Application.Abstractions.Authentication;
using Careersity.Domain.Identity;
using Careersity.Infrastructure.Authentication;
using Microsoft.AspNetCore.Identity;

namespace Careersity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCareersityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(x => !string.IsNullOrWhiteSpace(x.Issuer), "Jwt:Issuer is required.")
            .Validate(x => !string.IsNullOrWhiteSpace(x.Audience), "Jwt:Audience is required.")
            .Validate(x => System.Text.Encoding.UTF8.GetByteCount(x.SigningKey) >= 32,
                "Jwt:SigningKey must contain at least 32 bytes.")
            .Validate(x => x.AccessTokenLifetimeMinutes > 0, "JWT access-token lifetime must be positive.")
            .Validate(x => x.RefreshTokenLifetimeDays > 0, "JWT refresh-token lifetime must be positive.")
            .ValidateOnStart();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddSingleton<IPasswordService, PasswordService>();
        services.AddSingleton<IRefreshTokenService, RefreshTokenService>();
        services.AddSingleton<IAccessTokenService, AccessTokenService>();
        services.Configure<InitialAdminOptions>(configuration.GetSection(InitialAdminOptions.SectionName));
        services.AddScoped<InitialAdministratorInitializer>();

        var connectionString = configuration.GetConnectionString("CareersityDatabase");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<CareersityDbContext>(options => options.UseNpgsql(connectionString));
            services.AddScoped<ICareersityDbContext>(provider => provider.GetRequiredService<CareersityDbContext>());
        }
        else
        {
            services.AddScoped<ICareersityDbContext, UnavailableCareersityDbContext>();
        }

        return services;
    }
}
