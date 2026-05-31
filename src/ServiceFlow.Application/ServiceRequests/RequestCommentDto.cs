using ServiceFlow.Domain.ServiceRequests;

namespace ServiceFlow.Application.ServiceRequests;

public sealed record RequestCommentDto(
    Guid Id,
    Guid ServiceRequestId,
    Guid AuthorUserId,
    string Body,
    CommentVisibility Visibility,
    DateTimeOffset CreatedAtUtc);
