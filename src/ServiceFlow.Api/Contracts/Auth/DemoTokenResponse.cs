namespace ServiceFlow.Api.Contracts.Auth;

public sealed record DemoTokenResponse(
    string AccessToken,
    string TokenType,
    DateTimeOffset ExpiresAtUtc);
