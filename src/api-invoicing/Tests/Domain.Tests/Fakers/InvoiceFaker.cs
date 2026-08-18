using Domain.Entities;

namespace Domain.Tests.Fakers;

public static class InvoiceFaker
{
    public static InvoiceItem CreateValidItem(Guid? productId = null, int quantity = 2, decimal unitPrice = 50m)
    {
        return InvoiceItem.Create(
            productId ?? Guid.NewGuid(),
            "PRD-100",
            "Teclado Gamer",
            quantity,
            unitPrice
        ).Value!;
    }

    public static Invoice CreateValidInvoice(List<InvoiceItem>? items = null)
    {
        var itemList = items ?? [CreateValidItem()];
        return Invoice.Create(itemList).Value!;
    }
}
