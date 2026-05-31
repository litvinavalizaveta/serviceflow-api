using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceFlow.Api.Security;
using ServiceFlow.Application.ServiceRequests;

namespace ServiceFlow.Api.Controllers;

[ApiController]
[Route("api/service-requests/{serviceRequestId:guid}/audit-log")]
public sealed class ServiceRequestAuditLogController : ControllerBase
{
    private readonly IServiceRequestAuditLogService _auditLogService;

    public ServiceRequestAuditLogController(IServiceRequestAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    [HttpGet]
    [Authorize(Policy = ServiceFlowPolicies.CanManageServiceRequests)]
    [ProducesResponseType<IReadOnlyList<RequestAuditLogDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<RequestAuditLogDto>>> GetAuditLog(
        Guid serviceRequestId,
        CancellationToken cancellationToken)
    {
        return Ok(await _auditLogService.GetAuditLogAsync(serviceRequestId, cancellationToken));
    }
}
