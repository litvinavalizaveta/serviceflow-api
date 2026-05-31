namespace ServiceFlow.Application.ServiceRequests;

public sealed record RequestAuditLogDto(
    Guid Id,
    Guid ServiceRequestId,
    string Action,
    string? PreviousValue,
    string? NewValue,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAtUtc);
