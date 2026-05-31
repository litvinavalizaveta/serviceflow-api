namespace ServiceFlow.Api.Contracts.Clients;

public sealed record UpdateClientRequest(
    string Name,
    string Email,
    string CompanyName);
