using Domain.Common;

namespace Domain.Entities;

public sealed class InvoiceItem
{
    public Guid Id { get; private set; }
    public Guid InvoiceId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductCode { get; private set; } = string.Empty;
    public string ProductDescription { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal TotalPrice => Quantity * UnitPrice;

    private InvoiceItem() { }

    public static DomainResult<InvoiceItem> Create(
        Guid productId,
        string productCode,
        string productDescription,
        int quantity,
        decimal unitPrice)
    {
        if (productId == Guid.Empty)
            return "ID do produto inválido.";

        if (string.IsNullOrWhiteSpace(productCode))
            return "Código do produto é obrigatório.";

        if (string.IsNullOrWhiteSpace(productDescription))
            return "Descrição do produto é obrigatória.";

        if (quantity <= 0)
            return "A quantidade deve ser maior que zero.";

        if (unitPrice < 0)
            return "O preço unitário não pode ser negativo.";

        return DomainResult<InvoiceItem>.Success(new InvoiceItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            ProductCode = productCode.Trim().ToUpper(),
            ProductDescription = productDescription.Trim(),
            Quantity = quantity,
            UnitPrice = unitPrice
        });
    }

    internal void AttachToInvoice(Guid invoiceId)
    {
        InvoiceId = invoiceId;
    }
}
