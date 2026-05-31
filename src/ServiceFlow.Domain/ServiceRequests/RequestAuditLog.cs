using ServiceFlow.Domain.Common;

namespace ServiceFlow.Domain.ServiceRequests;

public sealed class RequestAuditLog
{
    private RequestAuditLog()
    {
        Action = string.Empty;
    }

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

    public Guid Id { get; private set; }

    public Guid ServiceRequestId { get; private set; }

    public string Action { get; private set; }

    public string? PreviousValue { get; private set; }

    public string? NewValue { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

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
