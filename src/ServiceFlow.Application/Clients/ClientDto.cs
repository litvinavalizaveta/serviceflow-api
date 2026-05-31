using ServiceFlow.Domain.Clients;

namespace ServiceFlow.Application.Clients;

public sealed record ClientDto(
    Guid Id,
    string Name,
    string Email,
    string CompanyName,
    ClientStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
