using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using ServiceFlow.Api.Contracts.Clients;
using ServiceFlow.Api.Contracts.ServiceRequests;
using ServiceFlow.Api.Security;
using ServiceFlow.Application.Clients;
using ServiceFlow.Application.ServiceRequests;
using ServiceFlow.Domain.ServiceRequests;
using ServiceFlow.IntegrationTests.Persistence;

namespace ServiceFlow.IntegrationTests.Api;

public sealed class ServiceRequestAuditLogApiTests : IClassFixture<PostgreSqlPersistenceFixture>
{
    private static readonly Guid AgentUserId = Guid.Parse("7d3395b9-464d-4bc3-8061-21887d8513f1");

    private readonly PostgreSqlPersistenceFixture _fixture;

    public ServiceRequestAuditLogApiTests(PostgreSqlPersistenceFixture fixture)
    {
        _fixture = fixture;
    }

    [DockerAvailableFact]
    public async Task Admin_CanGetAuditLogForServiceRequest()
    {
        using var client = ApiTestClientFactory.CreateClient(_fixture);
        await client.AuthenticateAsAsync(ServiceFlowRoles.Admin);
        var serviceRequest = await CreateServiceRequestWithStatusChangeAsync(client);

        var response = await client.GetAsync($"/api/service-requests/{serviceRequest.Id}/audit-log");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var auditLogs = await response.Content.ReadFromJsonAsync<List<RequestAuditLogDto>>(JsonOptions.Web);

        Assert.NotNull(auditLogs);
        Assert.Contains(auditLogs, log => log.Action == "StatusChanged");
    }

    [DockerAvailableFact]
    public async Task Agent_CanGetAuditLogForServiceRequest()
    {
        using var adminClient = ApiTestClientFactory.CreateClient(_fixture);
        await adminClient.AuthenticateAsAsync(ServiceFlowRoles.Admin);
        var serviceRequest = await CreateServiceRequestWithStatusChangeAsync(adminClient);

        using var agentClient = ApiTestClientFactory.CreateClient(_fixture);
        await agentClient.AuthenticateAsAsync(ServiceFlowRoles.Agent);

        var response = await agentClient.GetAsync($"/api/service-requests/{serviceRequest.Id}/audit-log");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var auditLogs = await response.Content.ReadFromJsonAsync<List<RequestAuditLogDto>>(JsonOptions.Web);

        Assert.NotNull(auditLogs);
        Assert.NotEmpty(auditLogs);
    }

    [DockerAvailableFact]
    public async Task Viewer_CannotGetAuditLog()
    {
        using var adminClient = ApiTestClientFactory.CreateClient(_fixture);
        await adminClient.AuthenticateAsAsync(ServiceFlowRoles.Admin);
        var serviceRequest = await CreateServiceRequestWithStatusChangeAsync(adminClient);

        using var viewerClient = ApiTestClientFactory.CreateClient(_fixture);
        await viewerClient.AuthenticateAsAsync(ServiceFlowRoles.Viewer);

        var response = await viewerClient.GetAsync($"/api/service-requests/{serviceRequest.Id}/audit-log");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [DockerAvailableFact]
    public async Task GetAuditLog_MissingServiceRequest_ReturnsProblemDetails()
    {
        using var client = ApiTestClientFactory.CreateClient(_fixture);
        await client.AuthenticateAsAsync(ServiceFlowRoles.Admin);

        var response = await client.GetAsync($"/api/service-requests/{Guid.NewGuid()}/audit-log");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions.Web);

        Assert.NotNull(problemDetails);
        Assert.Equal(404, problemDetails.Status);
    }

    [DockerAvailableFact]
    public async Task ChangeStatus_CreatesAuditLogEntry()
    {
        using var client = ApiTestClientFactory.CreateClient(_fixture);
        await client.AuthenticateAsAsync(ServiceFlowRoles.Admin);
        var serviceRequest = await CreateServiceRequestWithStatusChangeAsync(client);

        var response = await client.GetAsync($"/api/service-requests/{serviceRequest.Id}/audit-log");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var auditLogs = await response.Content.ReadFromJsonAsync<List<RequestAuditLogDto>>(JsonOptions.Web);

        Assert.NotNull(auditLogs);
        var statusChange = Assert.Single(auditLogs, log => log.Action == "StatusChanged");
        Assert.Equal(RequestStatus.New.ToString(), statusChange.PreviousValue);
        Assert.Equal(RequestStatus.InProgress.ToString(), statusChange.NewValue);
        Assert.Equal(AgentUserId, statusChange.CreatedByUserId);
    }

    private static async Task<ServiceRequestDto> CreateServiceRequestWithStatusChangeAsync(HttpClient client)
    {
        var serviceRequest = await CreateServiceRequestAsync(client);
        var response = await client.PostAsJsonAsync(
            $"/api/service-requests/{serviceRequest.Id}/status",
            new ChangeServiceRequestStatusRequest(RequestStatus.InProgress, AgentUserId),
            JsonOptions.Web);

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<ServiceRequestDto>(JsonOptions.Web))!;
    }

    private static async Task<ServiceRequestDto> CreateServiceRequestAsync(HttpClient client)
    {
        var createdClient = await CreateClientAsync(client);
        var response = await client.PostAsJsonAsync(
            "/api/service-requests",
            new CreateServiceRequestRequest(
                createdClient.Id,
                $"Audit workflow issue {Guid.NewGuid():N}",
                "The support team needs traceability for this request.",
                RequestPriority.High,
                DateTimeOffset.UtcNow.AddDays(2)),
            JsonOptions.Web);

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<ServiceRequestDto>(JsonOptions.Web))!;
    }

    private static async Task<ClientDto> CreateClientAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/api/clients",
            new CreateClientRequest("Audit Test Client", UniqueEmail("audit-client"), "Audit QA Co"),
            JsonOptions.Web);

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<ClientDto>(JsonOptions.Web))!;
    }

    private static string UniqueEmail(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}@example.com";
    }
}
