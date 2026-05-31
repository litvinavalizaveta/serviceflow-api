using ServiceFlow.Domain.ServiceRequests;

namespace ServiceFlow.UnitTests.ServiceRequests;

public sealed class RequestAuditLogTests
{
    [Fact]
    public void ForStatusChange_CreatesAuditLogWithPreviousAndNewValues()
    {
        var requestId = Guid.Parse("623b23d1-0fb2-46cb-a698-b8a58ec03c1d");
        var userId = Guid.Parse("d0d75796-994d-44fd-bfdf-bcba525af39e");
        var createdAt = new DateTimeOffset(2026, 05, 31, 12, 30, 0, TimeSpan.Zero);

        var auditLog = RequestAuditLog.ForStatusChange(
            requestId,
            RequestStatus.New,
            RequestStatus.InProgress,
            userId,
            createdAt);

        Assert.Equal(requestId, auditLog.ServiceRequestId);
        Assert.Equal("StatusChanged", auditLog.Action);
        Assert.Equal("New", auditLog.PreviousValue);
        Assert.Equal("InProgress", auditLog.NewValue);
        Assert.Equal(userId, auditLog.CreatedByUserId);
        Assert.Equal(createdAt, auditLog.CreatedAtUtc);
    }
}
