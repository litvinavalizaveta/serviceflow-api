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

public sealed class ServiceRequestCommentsApiTests : IClassFixture<PostgreSqlPersistenceFixture>
{
    private readonly PostgreSqlPersistenceFixture _fixture;

    public ServiceRequestCommentsApiTests(PostgreSqlPersistenceFixture fixture)
    {
        _fixture = fixture;
    }

    [DockerAvailableFact]
    public async Task Admin_CanAddInternalComment()
    {
        using var client = ApiTestClientFactory.CreateClient(_fixture);
        var adminUserId = Guid.NewGuid();
        await client.AuthenticateAsAsync(ServiceFlowRoles.Admin, adminUserId.ToString());
        var serviceRequest = await CreateServiceRequestAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/service-requests/{serviceRequest.Id}/comments",
            new AddRequestCommentRequest("Checked the customer import logs.", CommentVisibility.Internal),
            JsonOptions.Web);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var comment = await response.Content.ReadFromJsonAsync<RequestCommentDto>(JsonOptions.Web);

        Assert.NotNull(comment);
        Assert.Equal(serviceRequest.Id, comment.ServiceRequestId);
        Assert.Equal(adminUserId, comment.AuthorUserId);
        Assert.Equal(CommentVisibility.Internal, comment.Visibility);
    }

    [DockerAvailableFact]
    public async Task Agent_CanAddPublicComment()
    {
        using var adminClient = ApiTestClientFactory.CreateClient(_fixture);
        await adminClient.AuthenticateAsAsync(ServiceFlowRoles.Admin);
        var serviceRequest = await CreateServiceRequestAsync(adminClient);

        using var agentClient = ApiTestClientFactory.CreateClient(_fixture);
        await agentClient.AuthenticateAsAsync(ServiceFlowRoles.Agent);

        var response = await agentClient.PostAsJsonAsync(
            $"/api/service-requests/{serviceRequest.Id}/comments",
            new AddRequestCommentRequest("We are reviewing the latest customer update.", CommentVisibility.Public),
            JsonOptions.Web);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var comment = await response.Content.ReadFromJsonAsync<RequestCommentDto>(JsonOptions.Web);

        Assert.NotNull(comment);
        Assert.Equal(CommentVisibility.Public, comment.Visibility);
    }

    [DockerAvailableFact]
    public async Task Viewer_CannotAddComment()
    {
        using var adminClient = ApiTestClientFactory.CreateClient(_fixture);
        await adminClient.AuthenticateAsAsync(ServiceFlowRoles.Admin);
        var serviceRequest = await CreateServiceRequestAsync(adminClient);

        using var viewerClient = ApiTestClientFactory.CreateClient(_fixture);
        await viewerClient.AuthenticateAsAsync(ServiceFlowRoles.Viewer);

        var response = await viewerClient.PostAsJsonAsync(
            $"/api/service-requests/{serviceRequest.Id}/comments",
            new AddRequestCommentRequest("Customer-visible update.", CommentVisibility.Public),
            JsonOptions.Web);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [DockerAvailableFact]
    public async Task Viewer_CanListCommentsButSeesOnlyPublicComments()
    {
        using var adminClient = ApiTestClientFactory.CreateClient(_fixture);
        await adminClient.AuthenticateAsAsync(ServiceFlowRoles.Admin);
        var serviceRequest = await CreateServiceRequestAsync(adminClient);
        await AddCommentAsync(adminClient, serviceRequest.Id, "Internal triage note.", CommentVisibility.Internal);
        var publicComment = await AddCommentAsync(
            adminClient,
            serviceRequest.Id,
            "We are working on this request.",
            CommentVisibility.Public);

        using var viewerClient = ApiTestClientFactory.CreateClient(_fixture);
        await viewerClient.AuthenticateAsAsync(ServiceFlowRoles.Viewer);

        var response = await viewerClient.GetAsync($"/api/service-requests/{serviceRequest.Id}/comments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var comments = await response.Content.ReadFromJsonAsync<List<RequestCommentDto>>(JsonOptions.Web);

        Assert.NotNull(comments);
        Assert.Single(comments);
        Assert.Equal(publicComment.Id, comments[0].Id);
        Assert.Equal(CommentVisibility.Public, comments[0].Visibility);
    }

    [DockerAvailableFact]
    public async Task Agent_CanListCommentsAndSeesInternalComments()
    {
        using var adminClient = ApiTestClientFactory.CreateClient(_fixture);
        await adminClient.AuthenticateAsAsync(ServiceFlowRoles.Admin);
        var serviceRequest = await CreateServiceRequestAsync(adminClient);
        var internalComment = await AddCommentAsync(
            adminClient,
            serviceRequest.Id,
            "Internal reproduction steps.",
            CommentVisibility.Internal);
        await AddCommentAsync(adminClient, serviceRequest.Id, "Public customer update.", CommentVisibility.Public);

        using var agentClient = ApiTestClientFactory.CreateClient(_fixture);
        await agentClient.AuthenticateAsAsync(ServiceFlowRoles.Agent);

        var response = await agentClient.GetAsync($"/api/service-requests/{serviceRequest.Id}/comments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var comments = await response.Content.ReadFromJsonAsync<List<RequestCommentDto>>(JsonOptions.Web);

        Assert.NotNull(comments);
        Assert.Equal(2, comments.Count);
        Assert.Contains(comments, comment => comment.Id == internalComment.Id);
        Assert.Contains(comments, comment => comment.Visibility == CommentVisibility.Internal);
    }

    [DockerAvailableFact]
    public async Task AddComment_EmptyBody_ReturnsValidationProblemDetails()
    {
        using var client = ApiTestClientFactory.CreateClient(_fixture);
        await client.AuthenticateAsAsync(ServiceFlowRoles.Admin);
        var serviceRequest = await CreateServiceRequestAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/service-requests/{serviceRequest.Id}/comments",
            new AddRequestCommentRequest("", CommentVisibility.Public),
            JsonOptions.Web);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(JsonOptions.Web);

        Assert.NotNull(problemDetails);
        Assert.True(problemDetails.Errors.ContainsKey(nameof(AddRequestCommentRequest.Body)));
    }

    [DockerAvailableFact]
    public async Task GetComments_MissingServiceRequest_ReturnsProblemDetails()
    {
        using var client = ApiTestClientFactory.CreateClient(_fixture);
        await client.AuthenticateAsAsync(ServiceFlowRoles.Admin);

        var response = await client.GetAsync($"/api/service-requests/{Guid.NewGuid()}/comments");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions.Web);

        Assert.NotNull(problemDetails);
        Assert.Equal(404, problemDetails.Status);
    }

    private static async Task<RequestCommentDto> AddCommentAsync(
        HttpClient client,
        Guid serviceRequestId,
        string body,
        CommentVisibility visibility)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/service-requests/{serviceRequestId}/comments",
            new AddRequestCommentRequest(body, visibility),
            JsonOptions.Web);

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<RequestCommentDto>(JsonOptions.Web))!;
    }

    private static async Task<ServiceRequestDto> CreateServiceRequestAsync(HttpClient client)
    {
        var createdClient = await CreateClientAsync(client);
        var response = await client.PostAsJsonAsync(
            "/api/service-requests",
            new CreateServiceRequestRequest(
                createdClient.Id,
                $"Comment workflow issue {Guid.NewGuid():N}",
                "The support team needs to coordinate updates with the customer.",
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
            new CreateClientRequest("Comment Test Client", UniqueEmail("comment-client"), "Comment QA Co"),
            JsonOptions.Web);

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<ClientDto>(JsonOptions.Web))!;
    }

    private static string UniqueEmail(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}@example.com";
    }
}
