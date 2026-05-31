using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceFlow.Domain.Clients;
using ServiceFlow.Domain.ServiceRequests;

namespace ServiceFlow.Infrastructure.Persistence.Configurations;

public sealed class ServiceRequestConfiguration : IEntityTypeConfiguration<ServiceRequest>
{
    public void Configure(EntityTypeBuilder<ServiceRequest> builder)
    {
        builder.ToTable("service_requests");

        builder.HasKey(request => request.Id);

        builder.Property(request => request.Id)
            .ValueGeneratedNever();

        builder.Property(request => request.ClientId)
            .IsRequired();

        builder.HasOne<Client>()
            .WithMany()
            .HasForeignKey(request => request.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(request => request.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(request => request.Description)
            .IsRequired()
            .HasMaxLength(ServiceRequest.DescriptionMaxLength);

        builder.Property(request => request.Priority)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(request => request.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(request => request.DueDateUtc);

        builder.Property(request => request.ClosedAtUtc);

        builder.Property(request => request.CreatedAtUtc)
            .IsRequired();

        builder.Property(request => request.UpdatedAtUtc)
            .IsRequired();

        builder.Property(request => request.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .IsRowVersion();

        builder.HasMany(request => request.Comments)
            .WithOne()
            .HasForeignKey(comment => comment.ServiceRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(request => request.AuditLogs)
            .WithOne()
            .HasForeignKey(auditLog => auditLog.ServiceRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(request => request.Comments)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(request => request.AuditLogs)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(request => request.Status);
        builder.HasIndex(request => request.Priority);
        builder.HasIndex(request => request.ClientId);
        builder.HasIndex(request => request.CreatedAtUtc);
        builder.HasIndex(request => new { request.Status, request.Priority, request.CreatedAtUtc });
    }
}
