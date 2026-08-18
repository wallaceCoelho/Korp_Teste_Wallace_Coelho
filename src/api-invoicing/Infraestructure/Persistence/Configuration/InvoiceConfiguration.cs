using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infraestructure.Persistence.Configuration;

public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");

        builder.HasQueryFilter(i => i.DeletedAt == null);

        builder.Ignore(i => i.Blocked);

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Number)
            .HasDefaultValueSql("nextval('\"InvoiceNumberSequence\"')")
            .ValueGeneratedOnAdd();

        builder.HasIndex(i => i.Number)
            .IsUnique();

        builder.Property(i => i.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(i => i.TotalAmount)
            .HasPrecision(18, 4);

        builder.Property(i => i.CreatedAt)
            .IsRequired();

        builder.Property(i => i.UpdatedAt)
            .IsRequired(false);

        builder.Property(i => i.PrintedAt)
            .IsRequired(false);

        builder.Property(i => i.DeletedAt)
            .IsRequired(false);

        builder.HasMany(i => i.Items)
            .WithOne()
            .HasForeignKey(item => item.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(i => i.Version)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
    }
}
