using ServiceFlow.Domain.Clients;
using ServiceFlow.Domain.Common;

namespace ServiceFlow.UnitTests.Clients;

public sealed class ClientTests
{
    private static readonly DateTimeOffset Now = new(2026, 05, 31, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_WithoutName_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() =>
            new Client("", "client@example.com", "Acme", Now));

        Assert.Equal("Client name is required.", exception.Message);
    }

    [Fact]
    public void Create_WithInvalidEmail_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() =>
            new Client("Jane Customer", "not-an-email", "Acme", Now));

        Assert.Equal("Client email is invalid.", exception.Message);
    }

    [Fact]
    public void Archive_ActiveClient_MarksClientAsArchived()
    {
        var client = new Client("Jane Customer", "jane@example.com", "Acme", Now);
        var archivedAt = Now.AddHours(2);

        client.Archive(archivedAt);

        Assert.Equal(ClientStatus.Archived, client.Status);
        Assert.Equal(archivedAt, client.UpdatedAtUtc);
    }

    [Fact]
    public void Archive_AlreadyArchivedClient_DoesNotChangeUpdatedAt()
    {
        var client = new Client("Jane Customer", "jane@example.com", "Acme", Now);
        var firstArchiveTime = Now.AddHours(1);

        client.Archive(firstArchiveTime);
        client.Archive(Now.AddHours(2));

        Assert.Equal(ClientStatus.Archived, client.Status);
        Assert.Equal(firstArchiveTime, client.UpdatedAtUtc);
    }
}
