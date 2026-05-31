using ServiceFlow.Domain.Clients;
using ServiceFlow.Domain.Common;

namespace ServiceFlow.Domain.ServiceRequests;

public sealed class ServiceRequest
{
    public const int DescriptionMaxLength = 4_000;

    private readonly List<RequestComment> _comments = [];
    private readonly List<RequestAuditLog> _auditLogs = [];

    private ServiceRequest()
    {
        Title = string.Empty;
        Description = string.Empty;
    }

    private ServiceRequest(
        Guid clientId,
        string title,
        string description,
        RequestPriority priority,
        DateTimeOffset? dueDateUtc,
        DateTimeOffset createdAtUtc)
    {
        Id = Guid.NewGuid();
        ClientId = clientId;
        Title = RequireTitle(title);
        Description = NormalizeDescription(description);
        Priority = RequirePriority(priority);
        DueDateUtc = dueDateUtc;
        Status = RequestStatus.New;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid ClientId { get; private set; }

    public string Title { get; private set; }

    public string Description { get; private set; }

    public RequestPriority Priority { get; private set; }

    public RequestStatus Status { get; private set; }

    public DateTimeOffset? DueDateUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public DateTimeOffset? ClosedAtUtc { get; private set; }

    public uint RowVersion { get; private set; }

    public IReadOnlyCollection<RequestComment> Comments => _comments.AsReadOnly();

    public IReadOnlyCollection<RequestAuditLog> AuditLogs => _auditLogs.AsReadOnly();

    public static ServiceRequest CreateForClient(
        Client client,
        string title,
        string description,
        RequestPriority priority,
        DateTimeOffset? dueDateUtc = null,
        DateTimeOffset? createdAtUtc = null)
    {
        ArgumentNullException.ThrowIfNull(client);

        var now = createdAtUtc ?? DateTimeOffset.UtcNow;

        client.EnsureCanReceiveServiceRequest();
        EnsureValidDueDate(dueDateUtc, now);
        EnsurePriorityMatchesDueDate(priority, dueDateUtc);

        return new ServiceRequest(client.Id, title, description, priority, dueDateUtc, now);
    }

    public RequestAuditLog? ChangeStatus(
        RequestStatus newStatus,
        Guid changedByUserId,
        DateTimeOffset? changedAtUtc = null)
    {
        if (!Enum.IsDefined(newStatus))
        {
            throw new DomainException("Request status is invalid.");
        }

        if (Status == newStatus)
        {
            return null;
        }

        if (!CanTransitionTo(newStatus))
        {
            throw new DomainException($"Cannot change request status from {Status} to {newStatus}.");
        }

        var previousStatus = Status;
        var now = changedAtUtc ?? DateTimeOffset.UtcNow;

        Status = newStatus;
        UpdatedAtUtc = now;

        if (newStatus == RequestStatus.Closed)
        {
            ClosedAtUtc = now;
        }

        var auditLog = RequestAuditLog.ForStatusChange(Id, previousStatus, newStatus, changedByUserId, now);
        _auditLogs.Add(auditLog);

        return auditLog;
    }

    public RequestAuditLog? ChangePriority(
        RequestPriority newPriority,
        Guid changedByUserId,
        DateTimeOffset? changedAtUtc = null)
    {
        newPriority = RequirePriority(newPriority);
        EnsurePriorityMatchesDueDate(newPriority, DueDateUtc);

        if (Priority == newPriority)
        {
            return null;
        }

        var previousPriority = Priority;
        var now = changedAtUtc ?? DateTimeOffset.UtcNow;

        Priority = newPriority;
        UpdatedAtUtc = now;

        var auditLog = RequestAuditLog.ForPriorityChange(Id, previousPriority, newPriority, changedByUserId, now);
        _auditLogs.Add(auditLog);

        return auditLog;
    }

    public RequestAuditLog? Close(Guid closedByUserId, DateTimeOffset? closedAtUtc = null)
    {
        if (Status == RequestStatus.Closed)
        {
            return null;
        }

        return ChangeStatus(RequestStatus.Closed, closedByUserId, closedAtUtc);
    }

    public RequestComment AddComment(
        Guid authorUserId,
        string body,
        CommentVisibility visibility,
        DateTimeOffset? createdAtUtc = null)
    {
        var comment = new RequestComment(Id, authorUserId, body, visibility, createdAtUtc);
        _comments.Add(comment);
        return comment;
    }

    private bool CanTransitionTo(RequestStatus newStatus)
    {
        return Status switch
        {
            RequestStatus.New => newStatus is RequestStatus.InProgress
                or RequestStatus.WaitingForCustomer
                or RequestStatus.Resolved
                or RequestStatus.Closed,
            RequestStatus.InProgress => newStatus is RequestStatus.WaitingForCustomer
                or RequestStatus.Resolved
                or RequestStatus.Closed,
            RequestStatus.WaitingForCustomer => newStatus is RequestStatus.InProgress
                or RequestStatus.Resolved
                or RequestStatus.Closed,
            RequestStatus.Resolved => newStatus is RequestStatus.InProgress
                or RequestStatus.Closed,
            RequestStatus.Closed => false,
            _ => false
        };
    }

    private static string RequireTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Service request title is required.");
        }

        return title.Trim();
    }

    private static string NormalizeDescription(string description)
    {
        var normalizedDescription = description?.Trim() ?? string.Empty;

        if (normalizedDescription.Length > DescriptionMaxLength)
        {
            throw new DomainException($"Service request description cannot exceed {DescriptionMaxLength} characters.");
        }

        return normalizedDescription;
    }

    private static RequestPriority RequirePriority(RequestPriority priority)
    {
        if (!Enum.IsDefined(priority))
        {
            throw new DomainException("Request priority is invalid.");
        }

        return priority;
    }

    private static void EnsureValidDueDate(DateTimeOffset? dueDateUtc, DateTimeOffset createdAtUtc)
    {
        if (dueDateUtc < createdAtUtc)
        {
            throw new DomainException("Service request due date cannot be in the past.");
        }
    }

    private static void EnsurePriorityMatchesDueDate(RequestPriority priority, DateTimeOffset? dueDateUtc)
    {
        if (priority == RequestPriority.Critical && dueDateUtc is null)
        {
            throw new DomainException("Critical service requests must have a due date.");
        }
    }
}
