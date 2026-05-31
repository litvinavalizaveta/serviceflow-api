namespace ServiceFlow.Api.Contracts.Clients;

public sealed record CreateClientRequest(
    string Name,
    string Email,
    string CompanyName);
