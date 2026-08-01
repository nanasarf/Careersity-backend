using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Careersity.Infrastructure.Persistence;

namespace Careersity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCareersityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("CareersityDatabase");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<CareersityDbContext>(options => options.UseNpgsql(connectionString));
        }

        return services;
    }
}
