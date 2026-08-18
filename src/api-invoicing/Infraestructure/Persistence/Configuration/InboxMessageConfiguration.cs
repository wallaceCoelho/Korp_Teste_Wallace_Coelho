using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infraestructure.Persistence.Configuration;

public sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("InboxMessages");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id)
               .ValueGeneratedNever();

        builder.Property(o => o.Type)
               .HasMaxLength(200)
               .IsRequired();

        builder.Property(o => o.Content)
               .HasColumnType("jsonb")
               .IsRequired();

        builder.Property(o => o.ReceivedAt)
               .IsRequired();

        builder.Property(o => o.RetryCount)
               .HasDefaultValue(0)
               .IsRequired();

        builder.HasIndex(o => new { o.RetryCount, o.ReceivedAt });
    }
}
