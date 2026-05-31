using ServiceFlow.Domain.ServiceRequests;

namespace ServiceFlow.Application.ServiceRequests;

public sealed record CreateServiceRequestCommand(
    Guid ClientId,
    string Title,
    string Description,
    RequestPriority Priority,
    DateTimeOffset? DueDateUtc);
