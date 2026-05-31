using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServiceFlow.Infrastructure.Persistence;
using ServiceFlow.Infrastructure.Persistence.Seed;

namespace ServiceFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ServiceFlowDb");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'ServiceFlowDb' is not configured.");
        }

        services.AddDbContext<ServiceFlowDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(ServiceFlowDbContext).Assembly.FullName)));

        services.AddScoped<DevelopmentDataSeeder>();

        return services;
    }
}
