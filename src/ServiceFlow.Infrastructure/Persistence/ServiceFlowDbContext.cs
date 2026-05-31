using Microsoft.EntityFrameworkCore;
using ServiceFlow.Domain.Clients;
using ServiceFlow.Domain.ServiceRequests;

namespace ServiceFlow.Infrastructure.Persistence;

public sealed class ServiceFlowDbContext : DbContext
{
    public ServiceFlowDbContext(DbContextOptions<ServiceFlowDbContext> options)
        : base(options)
    {
    }

    public DbSet<Client> Clients => Set<Client>();

    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();

    public DbSet<RequestComment> RequestComments => Set<RequestComment>();

    public DbSet<RequestAuditLog> RequestAuditLogs => Set<RequestAuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ServiceFlowDbContext).Assembly);
    }
}
