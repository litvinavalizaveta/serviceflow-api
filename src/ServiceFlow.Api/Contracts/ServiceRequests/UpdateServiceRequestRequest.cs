using ServiceFlow.Domain.ServiceRequests;

namespace ServiceFlow.Api.Contracts.ServiceRequests;

public sealed record UpdateServiceRequestRequest(
    string Title,
    string Description,
    RequestPriority Priority,
    DateTimeOffset? DueDateUtc,
    Guid UpdatedByUserId);
