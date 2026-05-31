using ServiceFlow.Domain.ServiceRequests;

namespace ServiceFlow.Application.ServiceRequests;

public sealed record UpdateServiceRequestCommand(
    string Title,
    string Description,
    RequestPriority Priority,
    DateTimeOffset? DueDateUtc,
    Guid UpdatedByUserId);
