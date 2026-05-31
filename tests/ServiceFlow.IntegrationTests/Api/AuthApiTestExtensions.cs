using System.Net.Http.Headers;
using System.Net.Http.Json;
using ServiceFlow.Api.Contracts.Auth;

namespace ServiceFlow.IntegrationTests.Api;

public static class AuthApiTestExtensions
{
    public static async Task AuthenticateAsAsync(
        this HttpClient client,
        string role,
        string? userId = null,
        string? displayName = null)
    {
        var response = await client.PostAsJsonAsync(
            "/api/auth/demo-token",
            new DemoTokenRequest(
                userId ?? Guid.NewGuid().ToString(),
                displayName ?? $"Demo {role}",
                role),
            JsonOptions.Web);

        response.EnsureSuccessStatusCode();

        var token = await response.Content.ReadFromJsonAsync<DemoTokenResponse>(JsonOptions.Web);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            token!.TokenType,
            token.AccessToken);
    }
}
