using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using ServiceFlow.Api.Contracts.Clients;
using ServiceFlow.Api.Contracts.ServiceRequests;
using ServiceFlow.Application.Clients;
using ServiceFlow.Application.Common;
using ServiceFlow.Application.ServiceRequests;
using ServiceFlow.Domain.ServiceRequests;
using ServiceFlow.IntegrationTests.Persistence;

namespace ServiceFlow.IntegrationTests.Api;

public sealed class ServiceRequestsApiTests : IClassFixture<PostgreSqlPersistenceFixture>
{
    private static readonly Guid AgentUserId = Guid.Parse("b6df14e3-472f-4f2a-9156-5f87d5b915c5");

    private readonly PostgreSqlPersistenceFixture _fixture;

    public ServiceRequestsApiTests(PostgreSqlPersistenceFixture fixture)
    {
        _fixture = fixture;
    }

    [DockerAvailableFact]
    public async Task GetServiceRequests_ReturnsPaginatedRequests()
    {
        using var client = ApiTestClientFactory.CreateClient(_fixture);
        var createdRequest = await CreateServiceRequestAsync(client, RequestPriority.Medium);

        var response = await client.GetAsync($"/api/service-requests?clientId={createdRequest.ClientId}&page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<ServiceRequestDto>>(JsonOptions.Web);

        Assert.NotNull(result);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Contains(result.Items, item => item.Id == createdRequest.Id);
    }

    [DockerAvailableFact]
    public async Task PostServiceRequest_CreatesRequestAndReturnsCreated()
    {
        using var client = ApiTestClientFactory.CreateClient(_fixture);
        var createdClient = await CreateClientAsync(client);

        var response = await client.PostAsJsonAsync(
            "/api/service-requests",
            new CreateServiceRequestRequest(
                createdClient.Id,
                "Cannot export monthly report",
                "The export fails during the final packaging step.",
                RequestPriority.High,
                DateTimeOffset.UtcNow.AddDays(2)),
            JsonOptions.Web);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var createdRequest = await response.Content.ReadFromJsonAsync<ServiceRequestDto>(JsonOptions.Web);

        Assert.NotNull(createdRequest);
        Assert.Equal(createdClient.Id, createdRequest.ClientId);
        Assert.Equal(RequestStatus.New, createdRequest.Status);
    }

    [DockerAvailableFact]
    public async Task PostServiceRequest_ForArchivedClient_ReturnsProblemDetails()
    {
        using var client = ApiTestClientFactory.CreateClient(_fixture);
        var createdClient = await CreateClientAsync(client);
        var archiveResponse = await client.PostAsync($"/api/clients/{createdClient.Id}/archive", content: null);
        archiveResponse.EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync(
            "/api/service-requests",
            new CreateServiceRequestRequest(
                createdClient.Id,
                "Need audit history for closed requests",
                "Archived clients should not receive new work.",
                RequestPriority.Low,
                DateTimeOffset.UtcNow.AddDays(3)),
            JsonOptions.Web);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions.Web);

        Assert.NotNull(problemDetails);
        Assert.Equal(400, problemDetails.Status);
        Assert.Equal("Invalid request", problemDetails.Title);
    }

    [DockerAvailableFact]
    public async Task GetServiceRequest_MissingRequest_ReturnsProblemDetails()
    {
        using var client = ApiTestClientFactory.CreateClient(_fixture);

        var response = await client.GetAsync($"/api/service-requests/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions.Web);

        Assert.NotNull(problemDetails);
        Assert.Equal(404, problemDetails.Status);
    }

    [DockerAvailableFact]
    public async Task ChangeStatus_UpdatesStatus()
    {
        using var client = ApiTestClientFactory.CreateClient(_fixture);
        var createdRequest = await CreateServiceRequestAsync(client, RequestPriority.High);

        var response = await client.PostAsJsonAsync(
            $"/api/service-requests/{createdRequest.Id}/status",
            new ChangeServiceRequestStatusRequest(RequestStatus.InProgress, AgentUserId),
            JsonOptions.Web);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updatedRequest = await response.Content.ReadFromJsonAsync<ServiceRequestDto>(JsonOptions.Web);

        Assert.NotNull(updatedRequest);
        Assert.Equal(RequestStatus.InProgress, updatedRequest.Status);
    }

    [DockerAvailableFact]
    public async Task ChangeStatus_InvalidTransition_ReturnsProblemDetails()
    {
        using var client = ApiTestClientFactory.CreateClient(_fixture);
        var createdRequest = await CreateServiceRequestAsync(client, RequestPriority.Medium);
        var closeResponse = await client.PostAsJsonAsync(
            $"/api/service-requests/{createdRequest.Id}/close",
            new CloseServiceRequestRequest(AgentUserId.ToString()),
            JsonOptions.Web);
        closeResponse.EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync(
            $"/api/service-requests/{createdRequest.Id}/status",
            new ChangeServiceRequestStatusRequest(RequestStatus.New, AgentUserId),
            JsonOptions.Web);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions.Web);

        Assert.NotNull(problemDetails);
        Assert.Equal("Invalid request", problemDetails.Title);
    }

    [DockerAvailableFact]
    public async Task GetServiceRequests_CanFilterByStatus()
    {
        using var client = ApiTestClientFactory.CreateClient(_fixture);
        var createdRequest = await CreateServiceRequestAsync(client, RequestPriority.High);
        var statusResponse = await client.PostAsJsonAsync(
            $"/api/service-requests/{createdRequest.Id}/status",
            new ChangeServiceRequestStatusRequest(RequestStatus.InProgress, AgentUserId),
            JsonOptions.Web);
        statusResponse.EnsureSuccessStatusCode();

        var response = await client.GetAsync(
            $"/api/service-requests?clientId={createdRequest.ClientId}&status={RequestStatus.InProgress}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<ServiceRequestDto>>(JsonOptions.Web);

        Assert.NotNull(result);
        Assert.Contains(result.Items, item => item.Id == createdRequest.Id);
        Assert.All(result.Items, item => Assert.Equal(RequestStatus.InProgress, item.Status));
    }

    private static async Task<ClientDto> CreateClientAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/api/clients",
            new CreateClientRequest("Daniel Reed", UniqueEmail("request-client"), "Northwind Medical"),
            JsonOptions.Web);

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<ClientDto>(JsonOptions.Web))!;
    }

    private static async Task<ServiceRequestDto> CreateServiceRequestAsync(
        HttpClient client,
        RequestPriority priority)
    {
        var createdClient = await CreateClientAsync(client);
        var response = await client.PostAsJsonAsync(
            "/api/service-requests",
            new CreateServiceRequestRequest(
                createdClient.Id,
                $"API returns timeout during peak hours - {Guid.NewGuid():N}",
                "Partner API calls exceed the gateway timeout during the morning dispatch window.",
                priority,
                DateTimeOffset.UtcNow.AddDays(2)),
            JsonOptions.Web);

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<ServiceRequestDto>(JsonOptions.Web))!;
    }

    private static string UniqueEmail(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}@example.com";
    }
}
