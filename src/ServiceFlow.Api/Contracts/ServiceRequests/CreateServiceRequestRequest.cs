using ServiceFlow.Domain.ServiceRequests;

namespace ServiceFlow.Api.Contracts.ServiceRequests;

public sealed record CreateServiceRequestRequest(
    Guid ClientId,
    string Title,
    string Description,
    RequestPriority Priority,
    DateTimeOffset? DueDateUtc);
