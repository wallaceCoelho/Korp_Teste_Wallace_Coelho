using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infraestructure.Persistence.Configuration;

internal sealed class ProductReservationConfiguration : IEntityTypeConfiguration<ProductReservation>
{
    public void Configure(EntityTypeBuilder<ProductReservation> builder)
    {
        builder.ToTable("ProductReservations");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.InvoiceId)
            .IsRequired();

        builder.Property(r => r.Quantity)
            .IsRequired();

        builder.Property(r => r.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(r => r.ExpiresAt)
            .IsRequired();

        builder.HasIndex(r => new { r.ProductId, r.Status, r.ExpiresAt });

        builder.HasIndex(r => new { r.InvoiceId, r.ProductId })
            .IsUnique()
            .HasFilter($"\"Status\" = {(int)ReservationStatus.Pending}");

        builder.HasOne(r => r.Product)
            .WithMany(p => p.Reservations)
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
