using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceFlow.Api.Contracts.ServiceRequests;
using ServiceFlow.Api.Security;
using ServiceFlow.Application.ServiceRequests;

namespace ServiceFlow.Api.Controllers;

[ApiController]
[Route("api/service-requests/{serviceRequestId:guid}/comments")]
public sealed class ServiceRequestCommentsController : ControllerBase
{
    private readonly IServiceRequestCommentService _commentService;

    public ServiceRequestCommentsController(IServiceRequestCommentService commentService)
    {
        _commentService = commentService;
    }

    [HttpGet]
    [Authorize(Policy = ServiceFlowPolicies.CanReadServiceRequests)]
    [ProducesResponseType<IReadOnlyList<RequestCommentDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<RequestCommentDto>>> GetComments(
        Guid serviceRequestId,
        CancellationToken cancellationToken)
    {
        var comments = await _commentService.GetCommentsAsync(
            serviceRequestId,
            User.CanSeeInternalComments(),
            cancellationToken);

        return Ok(comments);
    }

    [HttpPost]
    [Authorize(Policy = ServiceFlowPolicies.CanManageServiceRequests)]
    [ProducesResponseType<RequestCommentDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RequestCommentDto>> AddComment(
        Guid serviceRequestId,
        AddRequestCommentRequest request,
        CancellationToken cancellationToken)
    {
        var comment = await _commentService.AddCommentAsync(
            serviceRequestId,
            new AddRequestCommentCommand(
                request.Body,
                request.Visibility,
                User.GetRequiredSubjectGuid()),
            cancellationToken);

        return CreatedAtAction(
            nameof(GetComments),
            new { serviceRequestId },
            comment);
    }
}
