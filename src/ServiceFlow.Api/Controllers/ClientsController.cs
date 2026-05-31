using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceFlow.Api.Contracts.Clients;
using ServiceFlow.Api.Security;
using ServiceFlow.Application.Clients;
using ServiceFlow.Application.Common;
using ServiceFlow.Domain.Clients;

namespace ServiceFlow.Api.Controllers;

[ApiController]
[Route("api/clients")]
public sealed class ClientsController : ControllerBase
{
    private readonly IClientService _clientService;

    public ClientsController(IClientService clientService)
    {
        _clientService = clientService;
    }

    [HttpGet]
    [Authorize(Policy = ServiceFlowPolicies.CanReadClients)]
    [ProducesResponseType<PagedResult<ClientDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ClientDto>>> GetClients(
        [FromQuery] int page = PageRequest.DefaultPage,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        [FromQuery] ClientStatus? status = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _clientService.GetClientsAsync(
            new ClientQueryParameters(status, search, new PageRequest(page, pageSize)),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = ServiceFlowPolicies.CanReadClients)]
    [ProducesResponseType<ClientDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClientDto>> GetClient(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await _clientService.GetClientByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = ServiceFlowPolicies.CanManageClients)]
    [ProducesResponseType<ClientDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ClientDto>> CreateClient(
        CreateClientRequest request,
        CancellationToken cancellationToken)
    {
        var client = await _clientService.CreateClientAsync(
            new CreateClientCommand(request.Name, request.Email, request.CompanyName),
            cancellationToken);

        return CreatedAtAction(nameof(GetClient), new { id = client.Id }, client);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = ServiceFlowPolicies.CanManageClients)]
    [ProducesResponseType<ClientDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ClientDto>> UpdateClient(
        Guid id,
        UpdateClientRequest request,
        CancellationToken cancellationToken)
    {
        var client = await _clientService.UpdateClientAsync(
            id,
            new UpdateClientCommand(request.Name, request.Email, request.CompanyName),
            cancellationToken);

        return Ok(client);
    }

    [HttpPost("{id:guid}/archive")]
    [Authorize(Policy = ServiceFlowPolicies.CanManageClients)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ArchiveClient(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _clientService.ArchiveClientAsync(id, cancellationToken);
        return NoContent();
    }
}
