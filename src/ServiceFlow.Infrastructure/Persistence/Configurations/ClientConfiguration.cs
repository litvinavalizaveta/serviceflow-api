using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceFlow.Domain.Clients;

namespace ServiceFlow.Infrastructure.Persistence.Configurations;

public sealed class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.ToTable("clients");

        builder.HasKey(client => client.Id);

        builder.Property(client => client.Id)
            .ValueGeneratedNever();

        builder.Property(client => client.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(client => client.Email)
            .IsRequired()
            .HasMaxLength(320);

        builder.Property(client => client.CompanyName)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(client => client.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(client => client.CreatedAtUtc)
            .IsRequired();

        builder.Property(client => client.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(client => client.Email)
            .IsUnique();

        builder.HasIndex(client => client.Status);
    }
}
