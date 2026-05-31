using Microsoft.EntityFrameworkCore;
using ServiceFlow.Infrastructure.Persistence;
using DotNet.Testcontainers.Builders;
using Testcontainers.PostgreSql;

namespace ServiceFlow.IntegrationTests.Persistence;

public sealed class PostgreSqlPersistenceFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;

    public bool IsAvailable { get; private set; }

    public string? SkipReason { get; private set; }

    public async Task InitializeAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder("postgres:16-alpine")
                .WithDatabase("serviceflow_tests")
                .WithUsername("serviceflow")
                .WithPassword("serviceflow")
                .Build();

            await _container.StartAsync();

            IsAvailable = true;

            await using var dbContext = CreateDbContext();
            await dbContext.Database.MigrateAsync();
        }
        catch (DockerUnavailableException ex)
        {
            SkipReason = $"Docker is not available for PostgreSQL integration tests: {ex.Message}";
        }
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    public ServiceFlowDbContext CreateDbContext()
    {
        if (!IsAvailable || _container is null)
        {
            throw new InvalidOperationException(SkipReason ?? "PostgreSQL test container is not available.");
        }

        var options = new DbContextOptionsBuilder<ServiceFlowDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;

        return new ServiceFlowDbContext(options);
    }
}
