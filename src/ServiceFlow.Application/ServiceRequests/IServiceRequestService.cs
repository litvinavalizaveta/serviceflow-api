using ServiceFlow.Application.Common;

namespace ServiceFlow.Application.ServiceRequests;

public interface IServiceRequestService
{
    Task<PagedResult<ServiceRequestDto>> GetServiceRequestsAsync(
        ServiceRequestQueryParameters query,
        CancellationToken cancellationToken);

    Task<ServiceRequestDto> GetServiceRequestByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<ServiceRequestDto> CreateServiceRequestAsync(
        CreateServiceRequestCommand command,
        CancellationToken cancellationToken);

    Task<ServiceRequestDto> UpdateServiceRequestAsync(
        Guid id,
        UpdateServiceRequestCommand command,
        CancellationToken cancellationToken);

    Task<ServiceRequestDto> ChangeStatusAsync(
        Guid id,
        ChangeServiceRequestStatusCommand command,
        CancellationToken cancellationToken);

    Task<ServiceRequestDto> CloseAsync(
        Guid id,
        string closedByUserId,
        CancellationToken cancellationToken);
}
