using Microsoft.EntityFrameworkCore;
using ServiceFlow.Application.Common;
using ServiceFlow.Application.ServiceRequests;
using ServiceFlow.Domain.Clients;
using ServiceFlow.Domain.Common;
using ServiceFlow.Domain.ServiceRequests;
using ServiceFlow.Infrastructure.Persistence;

namespace ServiceFlow.Infrastructure.ApplicationServices.ServiceRequests;

public sealed class ServiceRequestService : IServiceRequestService
{
    private readonly ServiceFlowDbContext _dbContext;

    public ServiceRequestService(ServiceFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<ServiceRequestDto>> GetServiceRequestsAsync(
        ServiceRequestQueryParameters query,
        CancellationToken cancellationToken)
    {
        var serviceRequests = ApplyFilters(_dbContext.ServiceRequests.AsNoTracking(), query);
        var totalCount = await serviceRequests.CountAsync(cancellationToken);

        var items = await (
                from request in serviceRequests
                join client in _dbContext.Clients.AsNoTracking()
                    on request.ClientId equals client.Id
                orderby request.CreatedAtUtc descending
                select new ServiceRequestDto(
                    request.Id,
                    request.ClientId,
                    client.Name,
                    request.Title,
                    request.Description,
                    request.Priority,
                    request.Status,
                    request.DueDateUtc,
                    request.CreatedAtUtc,
                    request.UpdatedAtUtc,
                    request.ClosedAtUtc))
            .Skip(query.PageRequest.Skip)
            .Take(query.PageRequest.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ServiceRequestDto>(
            items,
            query.PageRequest.Page,
            query.PageRequest.PageSize,
            totalCount);
    }

    public async Task<ServiceRequestDto> GetServiceRequestByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var serviceRequest = await (
                from request in _dbContext.ServiceRequests.AsNoTracking()
                join client in _dbContext.Clients.AsNoTracking()
                    on request.ClientId equals client.Id
                where request.Id == id
                select new ServiceRequestDto(
                    request.Id,
                    request.ClientId,
                    client.Name,
                    request.Title,
                    request.Description,
                    request.Priority,
                    request.Status,
                    request.DueDateUtc,
                    request.CreatedAtUtc,
                    request.UpdatedAtUtc,
                    request.ClosedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);

        return serviceRequest ?? throw new NotFoundException(nameof(ServiceRequest), id);
    }

    public async Task<ServiceRequestDto> CreateServiceRequestAsync(
        CreateServiceRequestCommand command,
        CancellationToken cancellationToken)
    {
        var client = await _dbContext.Clients
            .SingleOrDefaultAsync(client => client.Id == command.ClientId, cancellationToken);

        if (client is null)
        {
            throw new NotFoundException(nameof(Client), command.ClientId);
        }

        try
        {
            var serviceRequest = ServiceRequest.CreateForClient(
                client,
                command.Title,
                command.Description,
                command.Priority,
                command.DueDateUtc);

            _dbContext.ServiceRequests.Add(serviceRequest);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return ToDto(serviceRequest, client);
        }
        catch (DomainException ex)
        {
            throw new ForbiddenOperationException(ex.Message);
        }
    }

    public async Task<ServiceRequestDto> UpdateServiceRequestAsync(
        Guid id,
        UpdateServiceRequestCommand command,
        CancellationToken cancellationToken)
    {
        var serviceRequest = await FindServiceRequestAsync(id, cancellationToken);

        if (serviceRequest.Status == RequestStatus.Closed)
        {
            throw new ForbiddenOperationException("Closed service requests cannot be updated.");
        }

        try
        {
            serviceRequest.UpdateDetails(command.Title, command.Description, command.DueDateUtc);
            serviceRequest.ChangePriority(command.Priority, command.UpdatedByUserId);

            await _dbContext.SaveChangesAsync(cancellationToken);
            return await GetServiceRequestByIdAsync(id, cancellationToken);
        }
        catch (DomainException ex)
        {
            throw new ForbiddenOperationException(ex.Message);
        }
    }

    public async Task<ServiceRequestDto> ChangeStatusAsync(
        Guid id,
        ChangeServiceRequestStatusCommand command,
        CancellationToken cancellationToken)
    {
        var serviceRequest = await FindServiceRequestAsync(id, cancellationToken);

        try
        {
            serviceRequest.ChangeStatus(command.Status, command.ChangedByUserId);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return await GetServiceRequestByIdAsync(id, cancellationToken);
        }
        catch (DomainException ex)
        {
            throw new ForbiddenOperationException(ex.Message);
        }
    }

    public async Task<ServiceRequestDto> CloseAsync(
        Guid id,
        string closedByUserId,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(closedByUserId, out var userId))
        {
            throw new ForbiddenOperationException("Closed by user id must be a valid GUID.");
        }

        var serviceRequest = await FindServiceRequestAsync(id, cancellationToken);

        try
        {
            serviceRequest.Close(userId);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return await GetServiceRequestByIdAsync(id, cancellationToken);
        }
        catch (DomainException ex)
        {
            throw new ForbiddenOperationException(ex.Message);
        }
    }

    private static IQueryable<ServiceRequest> ApplyFilters(
        IQueryable<ServiceRequest> serviceRequests,
        ServiceRequestQueryParameters query)
    {
        if (query.Status is not null)
        {
            serviceRequests = serviceRequests.Where(request => request.Status == query.Status);
        }

        if (query.Priority is not null)
        {
            serviceRequests = serviceRequests.Where(request => request.Priority == query.Priority);
        }

        if (query.ClientId is not null)
        {
            serviceRequests = serviceRequests.Where(request => request.ClientId == query.ClientId);
        }

        if (query.CreatedFrom is not null)
        {
            serviceRequests = serviceRequests.Where(request => request.CreatedAtUtc >= query.CreatedFrom);
        }

        if (query.CreatedTo is not null)
        {
            serviceRequests = serviceRequests.Where(request => request.CreatedAtUtc <= query.CreatedTo);
        }

        return serviceRequests;
    }

    private async Task<ServiceRequest> FindServiceRequestAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var serviceRequest = await _dbContext.ServiceRequests
            .SingleOrDefaultAsync(request => request.Id == id, cancellationToken);

        return serviceRequest ?? throw new NotFoundException(nameof(ServiceRequest), id);
    }

    private static ServiceRequestDto ToDto(ServiceRequest request, Client client)
    {
        return new ServiceRequestDto(
            request.Id,
            request.ClientId,
            client.Name,
            request.Title,
            request.Description,
            request.Priority,
            request.Status,
            request.DueDateUtc,
            request.CreatedAtUtc,
            request.UpdatedAtUtc,
            request.ClosedAtUtc);
    }
}
