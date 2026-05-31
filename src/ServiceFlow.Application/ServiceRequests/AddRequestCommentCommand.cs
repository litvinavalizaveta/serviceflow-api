using ServiceFlow.Domain.ServiceRequests;

namespace ServiceFlow.Application.ServiceRequests;

public sealed record AddRequestCommentCommand(
    string Body,
    CommentVisibility Visibility,
    Guid AuthorUserId);
