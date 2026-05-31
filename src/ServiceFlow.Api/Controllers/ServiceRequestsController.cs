using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceFlow.Api.Contracts.ServiceRequests;
using ServiceFlow.Api.Security;
using ServiceFlow.Application.Common;
using ServiceFlow.Application.ServiceRequests;
using ServiceFlow.Domain.ServiceRequests;

namespace ServiceFlow.Api.Controllers;

[ApiController]
[Route("api/service-requests")]
public sealed class ServiceRequestsController : ControllerBase
{
    private readonly IServiceRequestService _serviceRequestService;

    public ServiceRequestsController(IServiceRequestService serviceRequestService)
    {
        _serviceRequestService = serviceRequestService;
    }

    [HttpGet]
    [Authorize(Policy = ServiceFlowPolicies.CanReadServiceRequests)]
    [ProducesResponseType<PagedResult<ServiceRequestDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ServiceRequestDto>>> GetServiceRequests(
        [FromQuery] int page = PageRequest.DefaultPage,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        [FromQuery] RequestStatus? status = null,
        [FromQuery] RequestPriority? priority = null,
        [FromQuery] Guid? clientId = null,
        [FromQuery] DateTimeOffset? createdFrom = null,
        [FromQuery] DateTimeOffset? createdTo = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _serviceRequestService.GetServiceRequestsAsync(
            new ServiceRequestQueryParameters(
                status,
                priority,
                clientId,
                createdFrom,
                createdTo,
                new PageRequest(page, pageSize)),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = ServiceFlowPolicies.CanReadServiceRequests)]
    [ProducesResponseType<ServiceRequestDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceRequestDto>> GetServiceRequest(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await _serviceRequestService.GetServiceRequestByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = ServiceFlowPolicies.CanManageServiceRequests)]
    [ProducesResponseType<ServiceRequestDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ServiceRequestDto>> CreateServiceRequest(
        CreateServiceRequestRequest request,
        CancellationToken cancellationToken)
    {
        var serviceRequest = await _serviceRequestService.CreateServiceRequestAsync(
            new CreateServiceRequestCommand(
                request.ClientId,
                request.Title,
                request.Description,
                request.Priority,
                request.DueDateUtc),
            cancellationToken);

        return CreatedAtAction(nameof(GetServiceRequest), new { id = serviceRequest.Id }, serviceRequest);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = ServiceFlowPolicies.CanManageServiceRequests)]
    [ProducesResponseType<ServiceRequestDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ServiceRequestDto>> UpdateServiceRequest(
        Guid id,
        UpdateServiceRequestRequest request,
        CancellationToken cancellationToken)
    {
        var serviceRequest = await _serviceRequestService.UpdateServiceRequestAsync(
            id,
            new UpdateServiceRequestCommand(
                request.Title,
                request.Description,
                request.Priority,
                request.DueDateUtc,
                request.UpdatedByUserId),
            cancellationToken);

        return Ok(serviceRequest);
    }

    [HttpPost("{id:guid}/status")]
    [Authorize(Policy = ServiceFlowPolicies.CanManageServiceRequests)]
    [ProducesResponseType<ServiceRequestDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ServiceRequestDto>> ChangeStatus(
        Guid id,
        ChangeServiceRequestStatusRequest request,
        CancellationToken cancellationToken)
    {
        var serviceRequest = await _serviceRequestService.ChangeStatusAsync(
            id,
            new ChangeServiceRequestStatusCommand(request.Status, request.ChangedByUserId),
            cancellationToken);

        return Ok(serviceRequest);
    }

    [HttpPost("{id:guid}/close")]
    [Authorize(Policy = ServiceFlowPolicies.CanManageServiceRequests)]
    [ProducesResponseType<ServiceRequestDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ServiceRequestDto>> Close(
        Guid id,
        CloseServiceRequestRequest request,
        CancellationToken cancellationToken)
    {
        var serviceRequest = await _serviceRequestService.CloseAsync(
            id,
            request.ClosedByUserId,
            cancellationToken);

        return Ok(serviceRequest);
    }
}
