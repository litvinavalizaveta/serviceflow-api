namespace ServiceFlow.Application.ServiceRequests;

public interface IServiceRequestCommentService
{
    Task<IReadOnlyList<RequestCommentDto>> GetCommentsAsync(
        Guid serviceRequestId,
        bool includeInternal,
        CancellationToken cancellationToken);

    Task<RequestCommentDto> AddCommentAsync(
        Guid serviceRequestId,
        AddRequestCommentCommand command,
        CancellationToken cancellationToken);
}
