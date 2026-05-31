using ServiceFlow.Domain.ServiceRequests;

namespace ServiceFlow.Api.Contracts.ServiceRequests;

public sealed record AddRequestCommentRequest(
    string Body,
    CommentVisibility Visibility);
