using Microsoft.EntityFrameworkCore;
using ServiceFlow.Domain.Clients;
using ServiceFlow.Domain.ServiceRequests;

namespace ServiceFlow.Infrastructure.Persistence.Seed;

public sealed class DevelopmentDataSeeder
{
    private static readonly Guid AdminUserId = Guid.Parse("7f83c261-b22a-4d8a-8417-4d02c7080247");
    private static readonly Guid AgentUserId = Guid.Parse("b6df14e3-472f-4f2a-9156-5f87d5b915c5");
    private static readonly Guid ViewerUserId = Guid.Parse("ca5acbde-273a-4d2a-8309-fb9cd80eed55");

    private readonly ServiceFlowDbContext _dbContext;

    public DevelopmentDataSeeder(ServiceFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.Database.MigrateAsync(cancellationToken);

        if (await _dbContext.Clients.AnyAsync(cancellationToken))
        {
            return;
        }

        var baseline = new DateTimeOffset(2026, 05, 15, 9, 0, 0, TimeSpan.Zero);

        var acme = new Client("Maya Chen", "support@acmelogistics.example", "Acme Logistics", baseline.AddDays(-12));
        var northwind = new Client("Daniel Reed", "it@northwindmedical.example", "Northwind Medical", baseline.AddDays(-9));
        var brightDesk = new Client("Sofia Bennett", "ops@brightdeskstudio.example", "BrightDesk Studio", baseline.AddDays(-6));

        var requests = new[]
        {
            CreateRequest(
                acme,
                "Cannot export monthly report",
                "The operations team receives a timeout when exporting the monthly shipment report.",
                RequestPriority.High,
                RequestStatus.InProgress,
                baseline.AddDays(-8),
                baseline.AddDays(2),
                "Export job moved to the async queue for investigation."),
            CreateRequest(
                acme,
                "API returns timeout during peak hours",
                "Partner API calls exceed the gateway timeout during the morning dispatch window.",
                RequestPriority.Critical,
                RequestStatus.WaitingForCustomer,
                baseline.AddDays(-7),
                baseline.AddHours(8),
                "Waiting for gateway logs from the client network team."),
            CreateRequest(
                acme,
                "Delivery notifications delayed",
                "SMS delivery notifications are arriving 30 minutes after route completion.",
                RequestPriority.Medium,
                RequestStatus.Resolved,
                baseline.AddDays(-6),
                baseline.AddDays(3),
                "Queue retry policy adjusted and verified in production logs."),
            CreateRequest(
                acme,
                "Need audit history for closed requests",
                "Operations managers need to review who closed older support requests.",
                RequestPriority.Low,
                RequestStatus.Closed,
                baseline.AddDays(-14),
                baseline.AddDays(-2),
                "Audit export provided for the requested date range."),
            CreateRequest(
                northwind,
                "Incorrect client status after import",
                "Several client records were marked archived after a CSV import.",
                RequestPriority.High,
                RequestStatus.InProgress,
                baseline.AddDays(-5),
                baseline.AddDays(1),
                "Import mapping confirmed; patch script is being reviewed."),
            CreateRequest(
                northwind,
                "Cannot attach lab results",
                "PDF attachments fail for files larger than 10 MB.",
                RequestPriority.Medium,
                RequestStatus.New,
                baseline.AddDays(-2),
                baseline.AddDays(5),
                "Initial reproduction details captured."),
            CreateRequest(
                northwind,
                "Notification email missing patient reference",
                "Automated notification emails do not include the expected external reference.",
                RequestPriority.Low,
                RequestStatus.Resolved,
                baseline.AddDays(-10),
                baseline.AddDays(-1),
                "Template updated and verified with sample payloads."),
            CreateRequest(
                brightDesk,
                "Unable to reopen resolved ticket",
                "Studio managers need clarification on how resolved requests can be revised.",
                RequestPriority.Medium,
                RequestStatus.WaitingForCustomer,
                baseline.AddDays(-4),
                baseline.AddDays(4),
                "Sent workflow explanation and asked for approval on policy change."),
            CreateRequest(
                brightDesk,
                "Dashboard count does not match request list",
                "The open request counter differs from the filtered request list by one item.",
                RequestPriority.High,
                RequestStatus.InProgress,
                baseline.AddDays(-3),
                baseline.AddDays(2),
                "Found stale cache entry; fix is ready for QA."),
            CreateRequest(
                brightDesk,
                "Request comments need internal-only notes",
                "Agents need a safe place for private troubleshooting context.",
                RequestPriority.Low,
                RequestStatus.New,
                baseline.AddDays(-1),
                baseline.AddDays(7),
                "Captured as a product workflow requirement.")
        };

        await _dbContext.Clients.AddRangeAsync([acme, northwind, brightDesk], cancellationToken);
        await _dbContext.ServiceRequests.AddRangeAsync(requests, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static ServiceRequest CreateRequest(
        Client client,
        string title,
        string description,
        RequestPriority priority,
        RequestStatus targetStatus,
        DateTimeOffset createdAtUtc,
        DateTimeOffset dueDateUtc,
        string publicComment)
    {
        var request = ServiceRequest.CreateForClient(
            client,
            title,
            description,
            priority,
            dueDateUtc,
            createdAtUtc);

        request.AddComment(ViewerUserId, publicComment, CommentVisibility.Public, createdAtUtc.AddHours(2));

        if (priority is RequestPriority.High or RequestPriority.Critical)
        {
            request.AddComment(
                AgentUserId,
                "Internal triage completed; priority confirmed against client impact.",
                CommentVisibility.Internal,
                createdAtUtc.AddHours(3));
        }

        if (targetStatus != RequestStatus.New)
        {
            request.ChangeStatus(NextStatusAfterNew(targetStatus), AgentUserId, createdAtUtc.AddHours(4));
        }

        if (targetStatus == RequestStatus.Resolved)
        {
            request.ChangeStatus(RequestStatus.Resolved, AgentUserId, createdAtUtc.AddDays(1));
        }
        else if (targetStatus == RequestStatus.Closed)
        {
            request.ChangeStatus(RequestStatus.InProgress, AgentUserId, createdAtUtc.AddHours(4));
            request.ChangeStatus(RequestStatus.Resolved, AgentUserId, createdAtUtc.AddDays(1));
            request.Close(AdminUserId, createdAtUtc.AddDays(2));
        }

        return request;
    }

    private static RequestStatus NextStatusAfterNew(RequestStatus targetStatus)
    {
        return targetStatus switch
        {
            RequestStatus.InProgress => RequestStatus.InProgress,
            RequestStatus.WaitingForCustomer => RequestStatus.WaitingForCustomer,
            RequestStatus.Resolved => RequestStatus.InProgress,
            RequestStatus.Closed => RequestStatus.InProgress,
            _ => targetStatus
        };
    }
}
