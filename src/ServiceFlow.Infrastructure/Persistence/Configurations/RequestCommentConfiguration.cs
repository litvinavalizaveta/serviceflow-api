using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceFlow.Domain.ServiceRequests;

namespace ServiceFlow.Infrastructure.Persistence.Configurations;

public sealed class RequestCommentConfiguration : IEntityTypeConfiguration<RequestComment>
{
    public void Configure(EntityTypeBuilder<RequestComment> builder)
    {
        builder.ToTable("request_comments");

        builder.HasKey(comment => comment.Id);

        builder.Property(comment => comment.Id)
            .ValueGeneratedNever();

        builder.Property(comment => comment.ServiceRequestId)
            .IsRequired();

        builder.Property(comment => comment.AuthorUserId)
            .IsRequired();

        builder.Property(comment => comment.Body)
            .IsRequired()
            .HasMaxLength(2_000);

        builder.Property(comment => comment.Visibility)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(comment => comment.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(comment => comment.ServiceRequestId);
    }
}
