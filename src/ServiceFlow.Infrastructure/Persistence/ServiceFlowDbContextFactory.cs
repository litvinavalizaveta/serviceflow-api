using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ServiceFlow.Infrastructure.Persistence;

public sealed class ServiceFlowDbContextFactory : IDesignTimeDbContextFactory<ServiceFlowDbContext>
{
    private const string LocalDevelopmentConnectionString =
        "Host=localhost;Port=5432;Database=serviceflow;Username=serviceflow;Password=serviceflow";

    public ServiceFlowDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__ServiceFlowDb")
            ?? LocalDevelopmentConnectionString;

        var options = new DbContextOptionsBuilder<ServiceFlowDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new ServiceFlowDbContext(options);
    }
}
