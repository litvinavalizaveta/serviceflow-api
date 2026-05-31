using ServiceFlow.Domain.ServiceRequests;

namespace ServiceFlow.Api.Contracts.ServiceRequests;

public sealed record ChangeServiceRequestStatusRequest(
    RequestStatus Status,
    Guid ChangedByUserId);
