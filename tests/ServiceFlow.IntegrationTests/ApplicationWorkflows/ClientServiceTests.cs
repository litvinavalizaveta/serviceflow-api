using Microsoft.EntityFrameworkCore;
using ServiceFlow.Application.Clients;
using ServiceFlow.Application.Common;
using ServiceFlow.Domain.Clients;
using ServiceFlow.Infrastructure.ApplicationServices.Clients;
using ServiceFlow.IntegrationTests.Persistence;

namespace ServiceFlow.IntegrationTests.ApplicationWorkflows;

public sealed class ClientServiceTests : IClassFixture<PostgreSqlPersistenceFixture>
{
    private readonly PostgreSqlPersistenceFixture _fixture;

    public ClientServiceTests(PostgreSqlPersistenceFixture fixture)
    {
        _fixture = fixture;
    }

    [DockerAvailableFact]
    public async Task CreateClientAsync_CreatesClient()
    {
        await using var dbContext = _fixture.CreateDbContext();
        var service = new ClientService(dbContext);
        var email = UniqueEmail("create-client");

        var client = await service.CreateClientAsync(
            new CreateClientCommand("Maya Chen", email, "Acme Logistics"),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, client.Id);
        Assert.Equal("Maya Chen", client.Name);
        Assert.Equal(email, client.Email);

        var savedClient = await dbContext.Clients.SingleAsync(saved => saved.Id == client.Id);
        Assert.Equal(ClientStatus.Active, savedClient.Status);
    }

    [DockerAvailableFact]
    public async Task ArchiveClientAsync_ArchivesClient()
    {
        await using var dbContext = _fixture.CreateDbContext();
        var service = new ClientService(dbContext);
        var client = await service.CreateClientAsync(
            new CreateClientCommand("Daniel Reed", UniqueEmail("archive-client"), "Northwind Medical"),
            CancellationToken.None);

        await service.ArchiveClientAsync(client.Id, CancellationToken.None);

        dbContext.ChangeTracker.Clear();
        var archivedClient = await dbContext.Clients.SingleAsync(saved => saved.Id == client.Id);
        Assert.Equal(ClientStatus.Archived, archivedClient.Status);
    }

    [DockerAvailableFact]
    public async Task GetClientByIdAsync_MissingClient_ThrowsNotFoundException()
    {
        await using var dbContext = _fixture.CreateDbContext();
        var service = new ClientService(dbContext);
        var missingId = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            service.GetClientByIdAsync(missingId, CancellationToken.None));

        Assert.Equal(nameof(Client), exception.ResourceName);
        Assert.Equal(missingId, exception.Id);
    }

    [DockerAvailableFact]
    public async Task GetClientsAsync_ReturnsPaginationMetadata()
    {
        await using var dbContext = _fixture.CreateDbContext();
        var service = new ClientService(dbContext);
        var prefix = $"page-{Guid.NewGuid():N}";

        await service.CreateClientAsync(
            new CreateClientCommand($"{prefix} Client 1", UniqueEmail("page-1"), "BrightDesk Studio"),
            CancellationToken.None);
        await service.CreateClientAsync(
            new CreateClientCommand($"{prefix} Client 2", UniqueEmail("page-2"), "BrightDesk Studio"),
            CancellationToken.None);
        await service.CreateClientAsync(
            new CreateClientCommand($"{prefix} Client 3", UniqueEmail("page-3"), "BrightDesk Studio"),
            CancellationToken.None);

        var result = await service.GetClientsAsync(
            new ClientQueryParameters(search: prefix, pageRequest: new PageRequest(page: 2, pageSize: 2)),
            CancellationToken.None);

        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(3, result.TotalCount);
        Assert.Single(result.Items);
    }

    private static string UniqueEmail(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}@example.com";
    }
}
