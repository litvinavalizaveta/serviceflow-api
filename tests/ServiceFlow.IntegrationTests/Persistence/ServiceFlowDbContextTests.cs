using Microsoft.EntityFrameworkCore;
using ServiceFlow.Domain.Clients;
using ServiceFlow.Domain.ServiceRequests;

namespace ServiceFlow.IntegrationTests.Persistence;

public sealed class ServiceFlowDbContextTests : IClassFixture<PostgreSqlPersistenceFixture>
{
    private static readonly DateTimeOffset Now = new(2026, 05, 31, 12, 0, 0, TimeSpan.Zero);

    private readonly PostgreSqlPersistenceFixture _fixture;

    public ServiceFlowDbContextTests(PostgreSqlPersistenceFixture fixture)
    {
        _fixture = fixture;
    }

    [DockerAvailableFact]
    public async Task SaveChanges_CanSaveAndLoadClient()
    {
        await using var dbContext = _fixture.CreateDbContext();
        var email = UniqueEmail("client");
        var client = new Client("Maya Chen", email, "Acme Logistics", Now);

        dbContext.Clients.Add(client);
        await dbContext.SaveChangesAsync();

        dbContext.ChangeTracker.Clear();

        var savedClient = await dbContext.Clients.SingleAsync(saved => saved.Email == email);

        Assert.Equal("Maya Chen", savedClient.Name);
        Assert.Equal("Acme Logistics", savedClient.CompanyName);
        Assert.Equal(ClientStatus.Active, savedClient.Status);
    }

    [DockerAvailableFact]
    public async Task SaveChanges_CanSaveServiceRequestForActiveClient()
    {
        await using var dbContext = _fixture.CreateDbContext();
        var client = new Client("Daniel Reed", UniqueEmail("request"), "Northwind Medical", Now);
        var request = ServiceRequest.CreateForClient(
            client,
            "API returns timeout during peak hours",
            "Partner API calls exceed the gateway timeout during the morning dispatch window.",
            RequestPriority.Critical,
            dueDateUtc: Now.AddHours(8),
            createdAtUtc: Now);

        dbContext.Clients.Add(client);
        dbContext.ServiceRequests.Add(request);
        await dbContext.SaveChangesAsync();

        dbContext.ChangeTracker.Clear();

        var savedRequest = await dbContext.ServiceRequests.SingleAsync(saved => saved.Id == request.Id);

        Assert.Equal(client.Id, savedRequest.ClientId);
        Assert.Equal(RequestPriority.Critical, savedRequest.Priority);
        Assert.Equal(RequestStatus.New, savedRequest.Status);
    }

    [DockerAvailableFact]
    public async Task SaveChanges_DuplicateClientEmail_ThrowsDbUpdateException()
    {
        await using var dbContext = _fixture.CreateDbContext();
        var email = UniqueEmail("duplicate");

        dbContext.Clients.AddRange(
            new Client("Sofia Bennett", email, "BrightDesk Studio", Now),
            new Client("Samuel Price", email, "BrightDesk Studio", Now));

        await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
    }

    private static string UniqueEmail(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}@example.com";
    }
}
