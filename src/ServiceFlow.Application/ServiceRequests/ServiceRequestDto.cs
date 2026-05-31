using ServiceFlow.Domain.ServiceRequests;

namespace ServiceFlow.Application.ServiceRequests;

public sealed record ServiceRequestDto(
    Guid Id,
    Guid ClientId,
    string? ClientName,
    string Title,
    string Description,
    RequestPriority Priority,
    RequestStatus Status,
    DateTimeOffset? DueDateUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? ClosedAtUtc);
