using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infraestructure.Persistence.Configuration;

public sealed class InvoiceItemConfiguration : IEntityTypeConfiguration<InvoiceItem>
{
    public void Configure(EntityTypeBuilder<InvoiceItem> builder)
    {
        builder.ToTable("InvoiceItems");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.InvoiceId)
            .IsRequired();

        builder.Property(i => i.ProductId)
            .IsRequired();

        builder.Property(i => i.ProductCode)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(i => i.ProductDescription)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(i => i.Quantity)
            .IsRequired();

        builder.Property(i => i.UnitPrice)
            .HasPrecision(18, 4);

        builder.Ignore(i => i.TotalPrice);
    }
}
