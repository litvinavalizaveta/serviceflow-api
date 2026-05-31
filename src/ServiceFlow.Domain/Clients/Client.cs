using System.Net.Mail;
using ServiceFlow.Domain.Common;

namespace ServiceFlow.Domain.Clients;

public sealed class Client
{
    private Client()
    {
        Name = string.Empty;
        Email = string.Empty;
        CompanyName = string.Empty;
    }

    public Client(string name, string email, string companyName, DateTimeOffset? createdAtUtc = null)
    {
        var now = createdAtUtc ?? DateTimeOffset.UtcNow;

        Id = Guid.NewGuid();
        Name = RequireName(name);
        Email = RequireEmail(email);
        CompanyName = companyName?.Trim() ?? string.Empty;
        Status = ClientStatus.Active;
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public string Email { get; private set; }

    public string CompanyName { get; private set; }

    public ClientStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public void Archive(DateTimeOffset? archivedAtUtc = null)
    {
        if (Status == ClientStatus.Archived)
        {
            return;
        }

        Status = ClientStatus.Archived;
        UpdatedAtUtc = archivedAtUtc ?? DateTimeOffset.UtcNow;
    }

    public void UpdateProfile(
        string name,
        string email,
        string companyName,
        DateTimeOffset? updatedAtUtc = null)
    {
        Name = RequireName(name);
        Email = RequireEmail(email);
        CompanyName = companyName?.Trim() ?? string.Empty;
        UpdatedAtUtc = updatedAtUtc ?? DateTimeOffset.UtcNow;
    }

    public void EnsureCanReceiveServiceRequest()
    {
        if (Status == ClientStatus.Archived)
        {
            throw new DomainException("Archived clients cannot receive new service requests.");
        }
    }

    private static string RequireName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Client name is required.");
        }

        return name.Trim();
    }

    private static string RequireEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("Client email is required.");
        }

        var trimmedEmail = email.Trim();

        if (!LooksLikeEmail(trimmedEmail))
        {
            throw new DomainException("Client email is invalid.");
        }

        return trimmedEmail;
    }

    private static bool LooksLikeEmail(string email)
    {
        try
        {
            var address = new MailAddress(email);
            return address.Address.Equals(email, StringComparison.OrdinalIgnoreCase)
                && address.Host.Contains('.', StringComparison.Ordinal);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
