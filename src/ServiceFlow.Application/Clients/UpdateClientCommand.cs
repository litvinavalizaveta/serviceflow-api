namespace ServiceFlow.Application.Clients;

public sealed record UpdateClientCommand(
    string Name,
    string Email,
    string CompanyName);
