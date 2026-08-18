using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infraestructure.Persistence.Configuration;

public sealed class InboxDeadLetterConfiguration : IEntityTypeConfiguration<InboxDeadLetter>
{
    public void Configure(EntityTypeBuilder<InboxDeadLetter> builder)
    {
        builder.ToTable("InboxDeadLetters");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();

        builder.Property(d => d.Type).HasMaxLength(200).IsRequired();
        builder.Property(d => d.Content).HasColumnType("jsonb").IsRequired();
        builder.Property(d => d.Error).HasColumnType("text").IsRequired();
        builder.Property(d => d.DeadLetteredAt).IsRequired();
        builder.Property(d => d.CompensatedAt).IsRequired(false);
        builder.Property(d => d.CompensationError).HasColumnType("text").IsRequired(false);

        builder.HasIndex(d => new { d.CompensatedAt, d.DeadLetteredAt });
    }
}
