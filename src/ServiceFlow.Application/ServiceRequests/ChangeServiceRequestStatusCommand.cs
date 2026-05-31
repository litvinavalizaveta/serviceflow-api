using ServiceFlow.Domain.ServiceRequests;

namespace ServiceFlow.Application.ServiceRequests;

public sealed record ChangeServiceRequestStatusCommand(
    RequestStatus Status,
    Guid ChangedByUserId);
