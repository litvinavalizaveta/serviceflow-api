using Microsoft.EntityFrameworkCore;
using ServiceFlow.Application.Clients;
using ServiceFlow.Application.Common;
using ServiceFlow.Domain.Clients;
using ServiceFlow.Infrastructure.Persistence;

namespace ServiceFlow.Infrastructure.ApplicationServices.Clients;

public sealed class ClientService : IClientService
{
    private readonly ServiceFlowDbContext _dbContext;

    public ClientService(ServiceFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<ClientDto>> GetClientsAsync(
        ClientQueryParameters query,
        CancellationToken cancellationToken)
    {
        var clients = _dbContext.Clients.AsNoTracking();

        if (query.Status is not null)
        {
            clients = clients.Where(client => client.Status == query.Status);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = $"%{query.Search.Trim()}%";
            clients = clients.Where(client =>
                EF.Functions.ILike(client.Name, search)
                || EF.Functions.ILike(client.Email, search)
                || EF.Functions.ILike(client.CompanyName, search));
        }

        var totalCount = await clients.CountAsync(cancellationToken);

        var items = await clients
            .OrderBy(client => client.CompanyName)
            .ThenBy(client => client.Name)
            .Skip(query.PageRequest.Skip)
            .Take(query.PageRequest.PageSize)
            .Select(client => ToDto(client))
            .ToListAsync(cancellationToken);

        return new PagedResult<ClientDto>(
            items,
            query.PageRequest.Page,
            query.PageRequest.PageSize,
            totalCount);
    }

    public async Task<ClientDto> GetClientByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var client = await _dbContext.Clients
            .AsNoTracking()
            .SingleOrDefaultAsync(client => client.Id == id, cancellationToken);

        return client is null
            ? throw new NotFoundException(nameof(Client), id)
            : ToDto(client);
    }

    public async Task<ClientDto> CreateClientAsync(
        CreateClientCommand command,
        CancellationToken cancellationToken)
    {
        var client = new Client(command.Name, command.Email, command.CompanyName);

        _dbContext.Clients.Add(client);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(client);
    }

    public async Task<ClientDto> UpdateClientAsync(
        Guid id,
        UpdateClientCommand command,
        CancellationToken cancellationToken)
    {
        var client = await FindClientAsync(id, cancellationToken);

        client.UpdateProfile(command.Name, command.Email, command.CompanyName);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(client);
    }

    public async Task ArchiveClientAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var client = await FindClientAsync(id, cancellationToken);

        client.Archive();
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Client> FindClientAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var client = await _dbContext.Clients
            .SingleOrDefaultAsync(client => client.Id == id, cancellationToken);

        return client ?? throw new NotFoundException(nameof(Client), id);
    }

    private static ClientDto ToDto(Client client)
    {
        return new ClientDto(
            client.Id,
            client.Name,
            client.Email,
            client.CompanyName,
            client.Status,
            client.CreatedAtUtc,
            client.UpdatedAtUtc);
    }
}
