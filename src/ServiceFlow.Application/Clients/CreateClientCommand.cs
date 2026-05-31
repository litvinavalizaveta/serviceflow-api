namespace ServiceFlow.Application.Clients;

public sealed record CreateClientCommand(
    string Name,
    string Email,
    string CompanyName);
