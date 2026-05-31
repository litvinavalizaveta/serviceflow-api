using ServiceFlow.Application.Common;

namespace ServiceFlow.Application.Clients;

public interface IClientService
{
    Task<PagedResult<ClientDto>> GetClientsAsync(
        ClientQueryParameters query,
        CancellationToken cancellationToken);

    Task<ClientDto> GetClientByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<ClientDto> CreateClientAsync(
        CreateClientCommand command,
        CancellationToken cancellationToken);

    Task<ClientDto> UpdateClientAsync(
        Guid id,
        UpdateClientCommand command,
        CancellationToken cancellationToken);

    Task ArchiveClientAsync(
        Guid id,
        CancellationToken cancellationToken);
}
