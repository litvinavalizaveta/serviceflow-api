using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ServiceFlow.Api.Contracts.Auth;
using ServiceFlow.Api.Contracts.Clients;
using ServiceFlow.Api.Contracts.ServiceRequests;
using ServiceFlow.Api.Security;
using ServiceFlow.Application.Clients;
using ServiceFlow.Application.ServiceRequests;
using ServiceFlow.Domain.ServiceRequests;
using ServiceFlow.IntegrationTests.Persistence;

namespace ServiceFlow.IntegrationTests.Api;

public sealed class AuthorizationApiTests : IClassFixture<PostgreSqlPersistenceFixture>
{
    private static readonly Guid AgentUserId = Guid.Parse("fda0d108-0e34-4980-8718-3df3cfc09e85");

    private readonly PostgreSqlPersistenceFixture _fixture;

    public AuthorizationApiTests(PostgreSqlPersistenceFixture fixture)
    {
        _fixture = fixture;
    }

    [DockerAvailableFact]
    public async Task Health_WorksWithoutToken()
    {
        using var client = ApiTestClientFactory.CreateClient(_fixture);

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [DockerAvailableFact]
    public async Task DemoToken_ValidRole_ReturnsToken()
    {
        using var client = ApiTestClientFactory.CreateClient(_fixture);

        var response = await client.PostAsJsonAsync(
            "/api/auth/demo-token",
            new DemoTokenRequest("demo-admin", "Demo Admin", ServiceFlowRoles.Admin),
            JsonOptions.Web);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var token = await response.Content.ReadFromJsonAsync<DemoTokenResponse>(JsonOptions.Web);

        Assert.NotNull(token);
        Assert.False(string.IsNullOrWhiteSpace(token.AccessToken));
        Assert.Equal("Bearer", token.TokenType);
        Assert.True(token.ExpiresAtUtc > DateTimeOffset.UtcNow);
    }

    [DockerAvailableFact]
    public async Task DemoToken_InvalidRole_ReturnsBadRequest()
    {
        using var client = ApiTestClientFactory.CreateClient(_fixture);

        var response = await client.PostAsJsonAsync(
            "/api/auth/demo-token",
            new DemoTokenRequest("demo-owner", "Demo Owner", "Owner"),
            JsonOptions.Web);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [DockerAvailableFact]
    public async Task GetClients_WithoutToken_ReturnsUnauthorized()
    {
        using var client = ApiTestClientFactory.CreateClient(_fixture);

        var response = await client.GetAsync("/api/clients");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [DockerAvailableFact]
    public async Task PostClient_WithoutToken_ReturnsUnauthorized()
    {
        using var client = ApiTestClientFactory.CreateClient(_fixture);

        var response = await client.PostAsJsonAsync(
            "/api/clients",
            new CreateClientRequest("Unauthenticated User", UniqueEmail("unauth-client"), "No Token Co"),
            JsonOptions.Web);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [DockerAvailableFact]
    public async Task GetClients_InvalidToken_ReturnsUnauthorized()
    {
        using var client = ApiTestClientFactory.CreateClient(_fixture);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-valid-token");

        var response = await client.GetAsync("/api/clients");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [DockerAvailableFact]
    public async Task Viewer_CanGetClientsButCannotCreateClient()
    {
        using var client = ApiTestClientFactory.CreateClient(_fixture);
        await client.AuthenticateAsAsync(ServiceFlowRoles.Viewer);

        var getResponse = await client.GetAsync("/api/clients");
        var postResponse = await client.PostAsJsonAsync(
            "/api/clients",
            new CreateClientRequest("Viewer User", UniqueEmail("viewer-client"), "Viewer Co"),
            JsonOptions.Web);

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, postResponse.StatusCode);
    }

    [DockerAvailableFact]
    public async Task Viewer_CanGetServiceRequestsButCannotCreateServiceRequest()
    {
        using var client = ApiTestClientFactory.CreateClient(_fixture);
        await client.AuthenticateAsAsync(ServiceFlowRoles.Viewer);

        var getResponse = await client.GetAsync("/api/service-requests");
        var postResponse = await client.PostAsJsonAsync(
            "/api/service-requests",
            NewServiceRequest(Guid.NewGuid()),
            JsonOptions.Web);

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, postResponse.StatusCode);
    }

    [DockerAvailableFact]
    public async Task Agent_CanGetClientsButCannotCreateClient()
    {
        using var client = ApiTestClientFactory.CreateClient(_fixture);
        await client.AuthenticateAsAsync(ServiceFlowRoles.Agent);

        var getResponse = await client.GetAsync("/api/clients");
        var postResponse = await client.PostAsJsonAsync(
            "/api/clients",
            new CreateClientRequest("Agent User", UniqueEmail("agent-client"), "Agent Co"),
            JsonOptions.Web);

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, postResponse.StatusCode);
    }

    [DockerAvailableFact]
    public async Task Agent_CanCreateAndChangeServiceRequest()
    {
        using var adminClient = ApiTestClientFactory.CreateClient(_fixture);
        await adminClient.AuthenticateAsAsync(ServiceFlowRoles.Admin);
        var createdClient = await CreateClientAsync(adminClient);

        using var agentClient = ApiTestClientFactory.CreateClient(_fixture);
        await agentClient.AuthenticateAsAsync(ServiceFlowRoles.Agent);

        var createResponse = await agentClient.PostAsJsonAsync(
            "/api/service-requests",
            NewServiceRequest(createdClient.Id),
            JsonOptions.Web);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createdRequest = await createResponse.Content.ReadFromJsonAsync<ServiceRequestDto>(JsonOptions.Web);

        var statusResponse = await agentClient.PostAsJsonAsync(
            $"/api/service-requests/{createdRequest!.Id}/status",
            new ChangeServiceRequestStatusRequest(RequestStatus.InProgress, AgentUserId),
            JsonOptions.Web);

        Assert.Equal(HttpStatusCode.OK, statusResponse.StatusCode);

        var updatedRequest = await statusResponse.Content.ReadFromJsonAsync<ServiceRequestDto>(JsonOptions.Web);

        Assert.NotNull(updatedRequest);
        Assert.Equal(RequestStatus.InProgress, updatedRequest.Status);
    }

    [DockerAvailableFact]
    public async Task Admin_CanCreateAndArchiveClient()
    {
        using var client = ApiTestClientFactory.CreateClient(_fixture);
        await client.AuthenticateAsAsync(ServiceFlowRoles.Admin);

        var createdClient = await CreateClientAsync(client);
        var archiveResponse = await client.PostAsync($"/api/clients/{createdClient.Id}/archive", content: null);

        Assert.Equal(HttpStatusCode.NoContent, archiveResponse.StatusCode);
    }

    [DockerAvailableFact]
    public async Task Admin_CanCreateAndCloseServiceRequest()
    {
        using var client = ApiTestClientFactory.CreateClient(_fixture);
        await client.AuthenticateAsAsync(ServiceFlowRoles.Admin);
        var createdClient = await CreateClientAsync(client);

        var createResponse = await client.PostAsJsonAsync(
            "/api/service-requests",
            NewServiceRequest(createdClient.Id),
            JsonOptions.Web);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createdRequest = await createResponse.Content.ReadFromJsonAsync<ServiceRequestDto>(JsonOptions.Web);
        var closeResponse = await client.PostAsJsonAsync(
            $"/api/service-requests/{createdRequest!.Id}/close",
            new CloseServiceRequestRequest(AgentUserId.ToString()),
            JsonOptions.Web);

        Assert.Equal(HttpStatusCode.OK, closeResponse.StatusCode);

        var closedRequest = await closeResponse.Content.ReadFromJsonAsync<ServiceRequestDto>(JsonOptions.Web);

        Assert.NotNull(closedRequest);
        Assert.Equal(RequestStatus.Closed, closedRequest.Status);
        Assert.NotNull(closedRequest.ClosedAtUtc);
    }

    private static async Task<ClientDto> CreateClientAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/api/clients",
            new CreateClientRequest("Authorization Test Client", UniqueEmail("auth-client"), "Auth Test Co"),
            JsonOptions.Web);

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<ClientDto>(JsonOptions.Web))!;
    }

    private static CreateServiceRequestRequest NewServiceRequest(Guid clientId)
    {
        return new CreateServiceRequestRequest(
            clientId,
            $"Authorization workflow issue {Guid.NewGuid():N}",
            "The customer needs a clear status update before tomorrow morning.",
            RequestPriority.High,
            DateTimeOffset.UtcNow.AddDays(2));
    }

    private static string UniqueEmail(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}@example.com";
    }
}
