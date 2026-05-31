using ServiceFlow.Domain.Common;

namespace ServiceFlow.Domain.ServiceRequests;

public sealed class RequestAuditLog
{
    public RequestAuditLog(
        Guid serviceRequestId,
        string action,
        string? previousValue,
        string? newValue,
        Guid createdByUserId,
        DateTimeOffset? createdAtUtc = null)
    {
        if (serviceRequestId == Guid.Empty)
        {
            throw new DomainException("Service request id is required for audit logs.");
        }

        if (string.IsNullOrWhiteSpace(action))
        {
            throw new DomainException("Audit action is required.");
        }

        if (createdByUserId == Guid.Empty)
        {
            throw new DomainException("Audit log creator is required.");
        }

        Id = Guid.NewGuid();
        ServiceRequestId = serviceRequestId;
        Action = action.Trim();
        PreviousValue = previousValue;
        NewValue = newValue;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = createdAtUtc ?? DateTimeOffset.UtcNow;
    }

    public Guid Id { get; }

    public Guid ServiceRequestId { get; }

    public string Action { get; }

    public string? PreviousValue { get; }

    public string? NewValue { get; }

    public Guid CreatedByUserId { get; }

    public DateTimeOffset CreatedAtUtc { get; }

    public static RequestAuditLog ForStatusChange(
        Guid serviceRequestId,
        RequestStatus previousStatus,
        RequestStatus newStatus,
        Guid createdByUserId,
        DateTimeOffset? createdAtUtc = null)
    {
        return new RequestAuditLog(
            serviceRequestId,
            "StatusChanged",
            previousStatus.ToString(),
            newStatus.ToString(),
            createdByUserId,
            createdAtUtc);
    }

    public static RequestAuditLog ForPriorityChange(
        Guid serviceRequestId,
        RequestPriority previousPriority,
        RequestPriority newPriority,
        Guid createdByUserId,
        DateTimeOffset? createdAtUtc = null)
    {
        return new RequestAuditLog(
            serviceRequestId,
            "PriorityChanged",
            previousPriority.ToString(),
            newPriority.ToString(),
            createdByUserId,
            createdAtUtc);
    }
}
