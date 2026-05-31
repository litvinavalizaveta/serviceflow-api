using Microsoft.EntityFrameworkCore;
using ServiceFlow.Application.Common;
using ServiceFlow.Application.ServiceRequests;
using ServiceFlow.Domain.ServiceRequests;
using ServiceFlow.Infrastructure.Persistence;

namespace ServiceFlow.Infrastructure.ApplicationServices.ServiceRequests;

public sealed class ServiceRequestAuditLogService : IServiceRequestAuditLogService
{
    private readonly ServiceFlowDbContext _dbContext;

    public ServiceRequestAuditLogService(ServiceFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<RequestAuditLogDto>> GetAuditLogAsync(
        Guid serviceRequestId,
        CancellationToken cancellationToken)
    {
        var exists = await _dbContext.ServiceRequests
            .AsNoTracking()
            .AnyAsync(request => request.Id == serviceRequestId, cancellationToken);

        if (!exists)
        {
            throw new NotFoundException(nameof(ServiceRequest), serviceRequestId);
        }

        return await _dbContext.RequestAuditLogs
            .AsNoTracking()
            .Where(auditLog => auditLog.ServiceRequestId == serviceRequestId)
            .OrderBy(auditLog => auditLog.CreatedAtUtc)
            .Select(auditLog => new RequestAuditLogDto(
                auditLog.Id,
                auditLog.ServiceRequestId,
                auditLog.Action,
                auditLog.PreviousValue,
                auditLog.NewValue,
                auditLog.CreatedByUserId,
                auditLog.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }
}
