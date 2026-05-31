using Microsoft.EntityFrameworkCore;
using ServiceFlow.Application.Clients;
using ServiceFlow.Application.Common;
using ServiceFlow.Application.ServiceRequests;
using ServiceFlow.Domain.Clients;
using ServiceFlow.Domain.ServiceRequests;
using ServiceFlow.Infrastructure.ApplicationServices.Clients;
using ServiceFlow.Infrastructure.ApplicationServices.ServiceRequests;
using ServiceFlow.IntegrationTests.Persistence;

namespace ServiceFlow.IntegrationTests.ApplicationWorkflows;

public sealed class ServiceRequestServiceTests : IClassFixture<PostgreSqlPersistenceFixture>
{
    private static readonly Guid AgentUserId = Guid.Parse("b6df14e3-472f-4f2a-9156-5f87d5b915c5");

    private readonly PostgreSqlPersistenceFixture _fixture;

    public ServiceRequestServiceTests(PostgreSqlPersistenceFixture fixture)
    {
        _fixture = fixture;
    }

    [DockerAvailableFact]
    public async Task CreateServiceRequestAsync_ForActiveClient_CreatesRequest()
    {
        await using var dbContext = _fixture.CreateDbContext();
        var client = await CreateClientAsync(dbContext, "active-request");
        var service = new ServiceRequestService(dbContext);

        var request = await service.CreateServiceRequestAsync(
            new CreateServiceRequestCommand(
                client.Id,
                "Cannot export monthly report",
                "The export fails during the final packaging step.",
                RequestPriority.High,
                DateTimeOffset.UtcNow.AddDays(2)),
            CancellationToken.None);

        Assert.Equal(client.Id, request.ClientId);
        Assert.Equal("Cannot export monthly report", request.Title);
        Assert.Equal(RequestStatus.New, request.Status);
        Assert.Equal(client.Name, request.ClientName);
    }

    [DockerAvailableFact]
    public async Task CreateServiceRequestAsync_ForArchivedClient_ThrowsForbiddenOperationException()
    {
        await using var dbContext = _fixture.CreateDbContext();
        var clientService = new ClientService(dbContext);
        var client = await clientService.CreateClientAsync(
            new CreateClientCommand("Archived Client", UniqueEmail("archived-request"), "Acme Logistics"),
            CancellationToken.None);
        await clientService.ArchiveClientAsync(client.Id, CancellationToken.None);

        var service = new ServiceRequestService(dbContext);

        var exception = await Assert.ThrowsAsync<ForbiddenOperationException>(() =>
            service.CreateServiceRequestAsync(
                new CreateServiceRequestCommand(
                    client.Id,
                    "Need audit history for closed requests",
                    "Archived clients should not receive new work.",
                    RequestPriority.Low,
                    DateTimeOffset.UtcNow.AddDays(3)),
                CancellationToken.None));

        Assert.Equal("Archived clients cannot receive new service requests.", exception.Message);
    }

    [DockerAvailableFact]
    public async Task GetServiceRequestsAsync_CanFilterByStatus()
    {
        await using var dbContext = _fixture.CreateDbContext();
        var client = await CreateClientAsync(dbContext, "filter-status");
        var service = new ServiceRequestService(dbContext);

        var newRequest = await CreateRequestAsync(service, client.Id, "filter new", RequestPriority.Low);
        var inProgressRequest = await CreateRequestAsync(service, client.Id, "filter in progress", RequestPriority.Medium);
        await service.ChangeStatusAsync(
            inProgressRequest.Id,
            new ChangeServiceRequestStatusCommand(RequestStatus.InProgress, AgentUserId),
            CancellationToken.None);

        var result = await service.GetServiceRequestsAsync(
            new ServiceRequestQueryParameters(
                status: RequestStatus.InProgress,
                clientId: client.Id,
                pageRequest: new PageRequest(page: 1, pageSize: 10)),
            CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(inProgressRequest.Id, result.Items.Single().Id);
        Assert.DoesNotContain(result.Items, item => item.Id == newRequest.Id);
    }

    [DockerAvailableFact]
    public async Task ChangeStatusAsync_ChangesStatusAndCreatesAuditEntry()
    {
        await using var dbContext = _fixture.CreateDbContext();
        var client = await CreateClientAsync(dbContext, "change-status");
        var service = new ServiceRequestService(dbContext);
        var request = await CreateRequestAsync(service, client.Id, "status audit", RequestPriority.High);

        var updatedRequest = await service.ChangeStatusAsync(
            request.Id,
            new ChangeServiceRequestStatusCommand(RequestStatus.InProgress, AgentUserId),
            CancellationToken.None);

        Assert.Equal(RequestStatus.InProgress, updatedRequest.Status);

        var auditLog = await dbContext.RequestAuditLogs.SingleAsync(log =>
            log.ServiceRequestId == request.Id
            && log.Action == "StatusChanged"
            && log.PreviousValue == "New"
            && log.NewValue == "InProgress");

        Assert.Equal(AgentUserId, auditLog.CreatedByUserId);
    }

    [DockerAvailableFact]
    public async Task CloseAsync_ClosesServiceRequest()
    {
        await using var dbContext = _fixture.CreateDbContext();
        var client = await CreateClientAsync(dbContext, "close-request");
        var service = new ServiceRequestService(dbContext);
        var request = await CreateRequestAsync(service, client.Id, "close request", RequestPriority.Medium);

        var closedRequest = await service.CloseAsync(request.Id, AgentUserId.ToString(), CancellationToken.None);

        Assert.Equal(RequestStatus.Closed, closedRequest.Status);
        Assert.NotNull(closedRequest.ClosedAtUtc);
    }

    private static async Task<ClientDto> CreateClientAsync(
        ServiceFlow.Infrastructure.Persistence.ServiceFlowDbContext dbContext,
        string emailPrefix)
    {
        var clientService = new ClientService(dbContext);
        return await clientService.CreateClientAsync(
            new CreateClientCommand(
                "Sofia Bennett",
                UniqueEmail(emailPrefix),
                "BrightDesk Studio"),
            CancellationToken.None);
    }

    private static async Task<ServiceRequestDto> CreateRequestAsync(
        ServiceRequestService service,
        Guid clientId,
        string titleSuffix,
        RequestPriority priority)
    {
        return await service.CreateServiceRequestAsync(
            new CreateServiceRequestCommand(
                clientId,
                $"API returns timeout during peak hours - {titleSuffix}",
                "Partner API calls exceed the gateway timeout during the morning dispatch window.",
                priority,
                DateTimeOffset.UtcNow.AddDays(2)),
            CancellationToken.None);
    }

    private static string UniqueEmail(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}@example.com";
    }
}
