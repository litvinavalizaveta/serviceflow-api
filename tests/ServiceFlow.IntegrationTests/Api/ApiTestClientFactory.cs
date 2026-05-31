using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ServiceFlow.Infrastructure.Persistence;
using ServiceFlow.IntegrationTests.Persistence;

namespace ServiceFlow.IntegrationTests.Api;

public static class ApiTestClientFactory
{
    public static HttpClient CreateClient(PostgreSqlPersistenceFixture fixture)
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:ServiceFlowDb"] = fixture.ConnectionString,
                        ["SeedData:RunOnStartup"] = "false"
                    });
                });

                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<DbContextOptions<ServiceFlowDbContext>>();
                    services.RemoveAll<ServiceFlowDbContext>();
                    services.AddDbContext<ServiceFlowDbContext>(options =>
                        options.UseNpgsql(fixture.ConnectionString));
                });
            });

        return factory.CreateClient();
    }
}
