namespace ServiceFlow.Api.Contracts.Auth;

public sealed record DemoTokenRequest(
    string UserId,
    string? DisplayName,
    string Role);
