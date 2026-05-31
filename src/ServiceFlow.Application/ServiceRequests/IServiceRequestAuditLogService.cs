namespace ServiceFlow.Application.ServiceRequests;

public interface IServiceRequestAuditLogService
{
    Task<IReadOnlyList<RequestAuditLogDto>> GetAuditLogAsync(
        Guid serviceRequestId,
        CancellationToken cancellationToken);
}
