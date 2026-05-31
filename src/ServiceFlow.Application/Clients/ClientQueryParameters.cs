using ServiceFlow.Application.Common;
using ServiceFlow.Domain.Clients;

namespace ServiceFlow.Application.Clients;

public sealed record ClientQueryParameters
{
    public ClientQueryParameters(
        ClientStatus? status = null,
        string? search = null,
        PageRequest? pageRequest = null)
    {
        Status = status;
        Search = search;
        PageRequest = pageRequest ?? new PageRequest();
    }

    public ClientStatus? Status { get; }

    public string? Search { get; }

    public PageRequest PageRequest { get; }
}
