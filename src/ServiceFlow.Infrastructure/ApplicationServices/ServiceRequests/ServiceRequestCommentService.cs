using Microsoft.EntityFrameworkCore;
using ServiceFlow.Application.Common;
using ServiceFlow.Application.ServiceRequests;
using ServiceFlow.Domain.ServiceRequests;
using ServiceFlow.Infrastructure.Persistence;

namespace ServiceFlow.Infrastructure.ApplicationServices.ServiceRequests;

public sealed class ServiceRequestCommentService : IServiceRequestCommentService
{
    private readonly ServiceFlowDbContext _dbContext;

    public ServiceRequestCommentService(ServiceFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<RequestCommentDto>> GetCommentsAsync(
        Guid serviceRequestId,
        bool includeInternal,
        CancellationToken cancellationToken)
    {
        await EnsureServiceRequestExistsAsync(serviceRequestId, cancellationToken);

        var comments = _dbContext.RequestComments
            .AsNoTracking()
            .Where(comment => comment.ServiceRequestId == serviceRequestId);

        if (!includeInternal)
        {
            comments = comments.Where(comment => comment.Visibility == CommentVisibility.Public);
        }

        return await comments
            .OrderBy(comment => comment.CreatedAtUtc)
            .Select(comment => new RequestCommentDto(
                comment.Id,
                comment.ServiceRequestId,
                comment.AuthorUserId,
                comment.Body,
                comment.Visibility,
                comment.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<RequestCommentDto> AddCommentAsync(
        Guid serviceRequestId,
        AddRequestCommentCommand command,
        CancellationToken cancellationToken)
    {
        var serviceRequest = await _dbContext.ServiceRequests
            .SingleOrDefaultAsync(request => request.Id == serviceRequestId, cancellationToken);

        if (serviceRequest is null)
        {
            throw new NotFoundException(nameof(ServiceRequest), serviceRequestId);
        }

        var comment = serviceRequest.AddComment(
            command.AuthorUserId,
            command.Body,
            command.Visibility);

        _dbContext.RequestComments.Add(comment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(comment);
    }

    private async Task EnsureServiceRequestExistsAsync(
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
    }

    private static RequestCommentDto ToDto(RequestComment comment)
    {
        return new RequestCommentDto(
            comment.Id,
            comment.ServiceRequestId,
            comment.AuthorUserId,
            comment.Body,
            comment.Visibility,
            comment.CreatedAtUtc);
    }
}
