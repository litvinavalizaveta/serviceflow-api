using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using ServiceFlow.Api.Contracts.Clients;
using ServiceFlow.Application.Clients;
using ServiceFlow.Application.Common;
using ServiceFlow.Domain.Clients;
using ServiceFlow.IntegrationTests.Persistence;

namespace ServiceFlow.IntegrationTests.Api;

public sealed class ClientsApiTests : IClassFixture<PostgreSqlPersistenceFixture>
{
    private readonly PostgreSqlPersistenceFixture _fixture;

    public ClientsApiTests(PostgreSqlPersistenceFixture fixture)
    {
        _fixture = fixture;
    }

    [DockerAvailableFact]
    public async Task GetClients_ReturnsPaginatedClients()
    {
        using var client = ApiTestClientFactory.CreateClient(_fixture);
        var createdClient = await CreateClientAsync(client, UniqueEmail("list-client"));

        var response = await client.GetAsync($"/api/clients?search={createdClient.Email}&page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<ClientDto>>(JsonOptions.Web);

        Assert.NotNull(result);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(createdClient.Id, result.Items.Single().Id);
    }

    [DockerAvailableFact]
    public async Task PostClient_CreatesClientAndReturnsCreated()
    {
        using var client = ApiTestClientFactory.CreateClient(_fixture);
        var email = UniqueEmail("post-client");

        var response = await client.PostAsJsonAsync(
            "/api/clients",
            new CreateClientRequest("Maya Chen", email, "Acme Logistics"),
            JsonOptions.Web);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var createdClient = await response.Content.ReadFromJsonAsync<ClientDto>(JsonOptions.Web);

        Assert.NotNull(createdClient);
        Assert.Equal(email, createdClient.Email);
        Assert.Equal(ClientStatus.Active, createdClient.Status);
    }

    [DockerAvailableFact]
    public async Task GetClient_ReturnsCreatedClient()
    {
        using var client = ApiTestClientFactory.CreateClient(_fixture);
        var createdClient = await CreateClientAsync(client, UniqueEmail("get-client"));

        var response = await client.GetAsync($"/api/clients/{createdClient.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var fetchedClient = await response.Content.ReadFromJsonAsync<ClientDto>(JsonOptions.Web);

        Assert.NotNull(fetchedClient);
        Assert.Equal(createdClient.Id, fetchedClient.Id);
        Assert.Equal(createdClient.Email, fetchedClient.Email);
    }

    [DockerAvailableFact]
    public async Task GetClient_MissingClient_ReturnsProblemDetails()
    {
        using var client = ApiTestClientFactory.CreateClient(_fixture);

        var response = await client.GetAsync($"/api/clients/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions.Web);

        Assert.NotNull(problemDetails);
        Assert.Equal(404, problemDetails.Status);
        Assert.Equal("Resource not found", problemDetails.Title);
    }

    [DockerAvailableFact]
    public async Task PostClient_InvalidClient_ReturnsValidationProblemDetails()
    {
        using var client = ApiTestClientFactory.CreateClient(_fixture);

        var response = await client.PostAsJsonAsync(
            "/api/clients",
            new CreateClientRequest("", "not-an-email", "Acme Logistics"),
            JsonOptions.Web);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(JsonOptions.Web);

        Assert.NotNull(problemDetails);
        Assert.Equal(400, problemDetails.Status);
        Assert.True(problemDetails.Errors.ContainsKey(nameof(CreateClientRequest.Name)));
        Assert.True(problemDetails.Errors.ContainsKey(nameof(CreateClientRequest.Email)));
    }

    [DockerAvailableFact]
    public async Task ArchiveClient_ReturnsNoContent()
    {
        using var client = ApiTestClientFactory.CreateClient(_fixture);
        var createdClient = await CreateClientAsync(client, UniqueEmail("archive-client"));

        var response = await client.PostAsync($"/api/clients/{createdClient.Id}/archive", content: null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private static async Task<ClientDto> CreateClientAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync(
            "/api/clients",
            new CreateClientRequest("Sofia Bennett", email, "BrightDesk Studio"),
            JsonOptions.Web);

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<ClientDto>(JsonOptions.Web))!;
    }

    private static string UniqueEmail(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}@example.com";
    }
}
