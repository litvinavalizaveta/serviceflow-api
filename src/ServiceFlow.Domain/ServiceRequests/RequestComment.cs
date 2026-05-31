using ServiceFlow.Domain.Common;

namespace ServiceFlow.Domain.ServiceRequests;

public sealed class RequestComment
{
    public RequestComment(
        Guid serviceRequestId,
        Guid authorUserId,
        string body,
        CommentVisibility visibility,
        DateTimeOffset? createdAtUtc = null)
    {
        if (serviceRequestId == Guid.Empty)
        {
            throw new DomainException("Service request id is required for comments.");
        }

        if (authorUserId == Guid.Empty)
        {
            throw new DomainException("Comment author is required.");
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            throw new DomainException("Comment body is required.");
        }

        if (!Enum.IsDefined(visibility))
        {
            throw new DomainException("Comment visibility is required.");
        }

        Id = Guid.NewGuid();
        ServiceRequestId = serviceRequestId;
        AuthorUserId = authorUserId;
        Body = body.Trim();
        Visibility = visibility;
        CreatedAtUtc = createdAtUtc ?? DateTimeOffset.UtcNow;
    }

    public Guid Id { get; }

    public Guid ServiceRequestId { get; }

    public Guid AuthorUserId { get; }

    public string Body { get; }

    public CommentVisibility Visibility { get; }

    public DateTimeOffset CreatedAtUtc { get; }
}
