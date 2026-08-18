using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infraestructure.Persistence.Configuration;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products", t =>
        {
            t.HasCheckConstraint("CK_Products_StockQuantity_NonNegative", "\"StockQuantity\" >= 0");
        });

        builder.HasQueryFilter(p => p.DeletedAt == null);

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Code)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(p => p.Code)
            .IsUnique()
            .HasFilter("\"DeletedAt\" IS NULL");

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(p => p.Description)
            .IsRequired(false)
            .HasMaxLength(500);

        builder.Property(p => p.StockQuantity)
            .IsRequired();

        builder.Property(p => p.MinStockQuantity)
            .IsRequired(false);

        builder.Property(p => p.UnitPrice)
            .HasPrecision(18, 4);

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .IsRequired(false);

        builder.Property(p => p.DeletedAt)
            .IsRequired(false);

        builder.Ignore(p => p.AvailableStockQuantity);

        builder.Navigation(p => p.Reservations)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasGeneratedTsVectorColumn(
            p => p.SearchVector,
            "portuguese",
            p => new { p.Name, p.Description, p.Code })
            .HasIndex(p => p.SearchVector)
            .HasMethod("GIN");

        builder.Property(p => p.Version)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
    }
}
