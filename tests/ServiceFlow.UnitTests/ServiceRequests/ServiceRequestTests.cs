using ServiceFlow.Domain.Clients;
using ServiceFlow.Domain.Common;
using ServiceFlow.Domain.ServiceRequests;

namespace ServiceFlow.UnitTests.ServiceRequests;

public sealed class ServiceRequestTests
{
    private static readonly DateTimeOffset Now = new(2026, 05, 31, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid AgentUserId = Guid.Parse("b6df14e3-472f-4f2a-9156-5f87d5b915c5");

    [Fact]
    public void Create_WithEmptyTitle_ThrowsDomainException()
    {
        var client = CreateClient();

        var exception = Assert.Throws<DomainException>(() =>
            ServiceRequest.CreateForClient(
                client,
                "",
                "VPN access is unavailable.",
                RequestPriority.High,
                dueDateUtc: Now.AddDays(1),
                createdAtUtc: Now));

        Assert.Equal("Service request title is required.", exception.Message);
    }

    [Fact]
    public void Create_WithDueDateInPast_ThrowsDomainException()
    {
        var client = CreateClient();

        var exception = Assert.Throws<DomainException>(() =>
            ServiceRequest.CreateForClient(
                client,
                "VPN outage",
                "VPN access is unavailable.",
                RequestPriority.High,
                dueDateUtc: Now.AddMinutes(-1),
                createdAtUtc: Now));

        Assert.Equal("Service request due date cannot be in the past.", exception.Message);
    }

    [Fact]
    public void Create_CriticalRequestWithoutDueDate_ThrowsDomainException()
    {
        var client = CreateClient();

        var exception = Assert.Throws<DomainException>(() =>
            ServiceRequest.CreateForClient(
                client,
                "Payroll system outage",
                "Payroll approval is blocked.",
                RequestPriority.Critical,
                createdAtUtc: Now));

        Assert.Equal("Critical service requests must have a due date.", exception.Message);
    }

    [Fact]
    public void Create_ForArchivedClient_ThrowsDomainException()
    {
        var client = CreateClient();
        client.Archive(Now.AddMinutes(1));

        var exception = Assert.Throws<DomainException>(() =>
            ServiceRequest.CreateForClient(
                client,
                "New laptop setup",
                "A new starter needs device setup.",
                RequestPriority.Medium,
                createdAtUtc: Now.AddMinutes(2)));

        Assert.Equal("Archived clients cannot receive new service requests.", exception.Message);
    }

    [Fact]
    public void Create_WithDescriptionLongerThanLimit_ThrowsDomainException()
    {
        var client = CreateClient();
        var description = new string('a', ServiceRequest.DescriptionMaxLength + 1);

        var exception = Assert.Throws<DomainException>(() =>
            ServiceRequest.CreateForClient(
                client,
                "Long request",
                description,
                RequestPriority.Low,
                createdAtUtc: Now));

        Assert.Equal("Service request description cannot exceed 4000 characters.", exception.Message);
    }

    [Fact]
    public void Close_OpenRequest_SetsClosedAtUtc()
    {
        var request = CreateServiceRequest();
        var closedAt = Now.AddHours(3);

        request.Close(AgentUserId, closedAt);

        Assert.Equal(RequestStatus.Closed, request.Status);
        Assert.Equal(closedAt, request.ClosedAtUtc);
    }

    [Fact]
    public void Close_AlreadyClosedRequest_DoesNotChangeClosedAtUtc()
    {
        var request = CreateServiceRequest();
        var firstClosedAt = Now.AddHours(3);

        request.Close(AgentUserId, firstClosedAt);
        var auditLog = request.Close(AgentUserId, Now.AddHours(4));

        Assert.Null(auditLog);
        Assert.Equal(firstClosedAt, request.ClosedAtUtc);
        Assert.Single(request.AuditLogs);
    }

    [Fact]
    public void ChangeStatus_ClosedRequestToNew_ThrowsDomainException()
    {
        var request = CreateServiceRequest();
        request.Close(AgentUserId, Now.AddHours(1));

        var exception = Assert.Throws<DomainException>(() =>
            request.ChangeStatus(RequestStatus.New, AgentUserId, Now.AddHours(2)));

        Assert.Equal("Cannot change request status from Closed to New.", exception.Message);
    }

    [Fact]
    public void ChangeStatus_NewRequestToInProgress_UpdatesStatusAndCreatesAuditLog()
    {
        var request = CreateServiceRequest();
        var changedAt = Now.AddMinutes(30);

        var auditLog = request.ChangeStatus(RequestStatus.InProgress, AgentUserId, changedAt);

        Assert.Equal(RequestStatus.InProgress, request.Status);
        Assert.Equal(changedAt, request.UpdatedAtUtc);
        Assert.NotNull(auditLog);
        Assert.Equal("StatusChanged", auditLog.Action);
        Assert.Equal("New", auditLog.PreviousValue);
        Assert.Equal("InProgress", auditLog.NewValue);
        Assert.Contains(auditLog, request.AuditLogs);
    }

    [Fact]
    public void AddComment_WithPublicVisibilityAndValidBody_AddsComment()
    {
        var request = CreateServiceRequest();
        var authorUserId = Guid.Parse("ca5acbde-273a-4d2a-8309-fb9cd80eed55");

        var comment = request.AddComment(
            authorUserId,
            "Customer confirmed the workaround.",
            CommentVisibility.Public,
            Now.AddMinutes(10));

        Assert.Equal(request.Id, comment.ServiceRequestId);
        Assert.Equal(authorUserId, comment.AuthorUserId);
        Assert.Equal(CommentVisibility.Public, comment.Visibility);
        Assert.Contains(comment, request.Comments);
    }

    [Fact]
    public void AddComment_WithEmptyBody_ThrowsDomainException()
    {
        var request = CreateServiceRequest();

        var exception = Assert.Throws<DomainException>(() =>
            request.AddComment(AgentUserId, " ", CommentVisibility.Public, Now.AddMinutes(10)));

        Assert.Equal("Comment body is required.", exception.Message);
    }

    private static Client CreateClient()
    {
        return new Client("Jane Customer", "jane@example.com", "Acme", Now);
    }

    private static ServiceRequest CreateServiceRequest()
    {
        return ServiceRequest.CreateForClient(
            CreateClient(),
            "VPN outage",
            "VPN access is unavailable for the finance team.",
            RequestPriority.High,
            dueDateUtc: Now.AddDays(1),
            createdAtUtc: Now);
    }
}
