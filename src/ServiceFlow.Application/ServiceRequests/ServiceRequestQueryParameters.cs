using ServiceFlow.Application.Common;
using ServiceFlow.Domain.ServiceRequests;

namespace ServiceFlow.Application.ServiceRequests;

public sealed record ServiceRequestQueryParameters
{
    public ServiceRequestQueryParameters(
        RequestStatus? status = null,
        RequestPriority? priority = null,
        Guid? clientId = null,
        DateTimeOffset? createdFrom = null,
        DateTimeOffset? createdTo = null,
        PageRequest? pageRequest = null)
    {
        Status = status;
        Priority = priority;
        ClientId = clientId;
        CreatedFrom = createdFrom;
        CreatedTo = createdTo;
        PageRequest = pageRequest ?? new PageRequest();
    }

    public RequestStatus? Status { get; }

    public RequestPriority? Priority { get; }

    public Guid? ClientId { get; }

    public DateTimeOffset? CreatedFrom { get; }

    public DateTimeOffset? CreatedTo { get; }

    public PageRequest PageRequest { get; }
}
