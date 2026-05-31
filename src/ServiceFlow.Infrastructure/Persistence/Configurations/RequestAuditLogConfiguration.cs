using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceFlow.Domain.ServiceRequests;

namespace ServiceFlow.Infrastructure.Persistence.Configurations;

public sealed class RequestAuditLogConfiguration : IEntityTypeConfiguration<RequestAuditLog>
{
    public void Configure(EntityTypeBuilder<RequestAuditLog> builder)
    {
        builder.ToTable("request_audit_logs");

        builder.HasKey(auditLog => auditLog.Id);

        builder.Property(auditLog => auditLog.Id)
            .ValueGeneratedNever();

        builder.Property(auditLog => auditLog.ServiceRequestId)
            .IsRequired();

        builder.Property(auditLog => auditLog.Action)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(auditLog => auditLog.PreviousValue)
            .HasMaxLength(200);

        builder.Property(auditLog => auditLog.NewValue)
            .HasMaxLength(200);

        builder.Property(auditLog => auditLog.CreatedByUserId)
            .IsRequired();

        builder.Property(auditLog => auditLog.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(auditLog => auditLog.ServiceRequestId);
        builder.HasIndex(auditLog => auditLog.CreatedAtUtc);
    }
}
