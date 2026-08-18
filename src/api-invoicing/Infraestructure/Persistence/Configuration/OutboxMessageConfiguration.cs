using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infraestructure.Persistence.Configuration;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Type)
               .HasMaxLength(200)
               .IsRequired();

        builder.Property(o => o.Content)
               .HasColumnType("jsonb")
               .IsRequired();

        builder.Property(o => o.CreatedAt)
               .IsRequired();

        builder.Property(o => o.RetryCount)
               .HasDefaultValue(0)
               .IsRequired();

        builder.HasIndex(o => new { o.RetryCount, o.CreatedAt });
    }
}
