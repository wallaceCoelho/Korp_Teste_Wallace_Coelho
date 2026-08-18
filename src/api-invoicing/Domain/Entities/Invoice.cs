using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public sealed class Invoice
{
    private readonly List<InvoiceItem> _items = [];

    public Guid Id { get; private set; }
    public long Number { get; private set; }
    public InvoiceStatus Status { get; private set; }
    public string? ReasonRejected { get; private set; }
    public IReadOnlyCollection<InvoiceItem> Items => _items.AsReadOnly();
    public decimal TotalAmount { get; private set; }

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? PrintedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    public bool Blocked => Status == InvoiceStatus.Canceled || Status == InvoiceStatus.Closed || Status == InvoiceStatus.Pending;

    public uint Version { get; private set; }

    private Invoice() { }

    public static DomainResult<Invoice> Create(List<InvoiceItem> items)
    {
        if (items is null || items.Count == 0)
            return "A nota fiscal deve conter pelo menos um produto.";

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            Status = InvoiceStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var item in items)
        {
            item.AttachToInvoice(invoice.Id);
            invoice._items.Add(item);
        }

        invoice.TotalAmount = invoice._items.Sum(i => i.TotalPrice);

        return DomainResult<Invoice>.Success(invoice);
    }

    public DomainResult UpdateItems(List<InvoiceItem> items)
    {
        if (DeletedAt.HasValue)
            return "Não é possível editar uma nota fiscal excluída.";

        if (Status != InvoiceStatus.Rejected && Status != InvoiceStatus.Open)
            return "Apenas notas fiscais rejeitadas ou abertas podem ser editadas e reenviadas.";

        if (items is null || items.Count == 0)
            return "A nota fiscal deve conter pelo menos um produto.";

        _items.Clear();
        foreach (var item in items)
        {
            item.AttachToInvoice(Id);
            _items.Add(item);
        }

        TotalAmount = _items.Sum(i => i.TotalPrice);
        Status = InvoiceStatus.Pending;
        ReasonRejected = null;
        UpdatedAt = DateTime.UtcNow;

        return DomainResult.Success();
    }

    public DomainResult SeeCanPrint()
    {
        if (DeletedAt.HasValue)
            return "Não é possível imprimir uma nota fiscal excluída.";

        if  (Status == InvoiceStatus.Closed)
            return "Não é possível imprimir uma nota fiscal fechada.";

        if (Status == InvoiceStatus.Canceled)
            return "Não é possível imprimir uma nota fiscal cancelada.";

        if (Status == InvoiceStatus.Pending)
            return "Não é possível imprimir uma nota fiscal pendente.";

        if (Status == InvoiceStatus.Rejected)
            return "Não é possível imprimir uma nota fiscal rejeitada.";

        return DomainResult.Success();
    }

    public DomainResult Open()
    {
        if (DeletedAt.HasValue)
            return "Não é possível abrir uma nota fiscal excluída.";

        if (Status != InvoiceStatus.Pending)
            return "Não é possível abrir uma nota fiscal que não esteja com status Pendente.";

        Status = InvoiceStatus.Open;
        UpdatedAt = DateTime.UtcNow;

        return DomainResult.Success();
    }

    public DomainResult CanCancel()
    {
        if (DeletedAt.HasValue)
            return "Não é possível cancelar uma nota fiscal excluída.";

        if (Status == InvoiceStatus.Closed || Status == InvoiceStatus.Canceled)
            return "Não é possível cancelar uma nota fiscal que esteja Cancelada ou Fechada.";

        return DomainResult.Success();
    }

    public DomainResult Cancel()
    {
        if (DeletedAt.HasValue)
            return "Não é possível cancelar uma nota fiscal excluída.";

        if (Status == InvoiceStatus.Closed || Status == InvoiceStatus.Canceled)
            return "Não é possível cancelar uma nota fiscal que esteja Cancelada ou Fechada.";

        Status = InvoiceStatus.Canceled;
        UpdatedAt = DateTime.UtcNow;

        return DomainResult.Success();
    }

    public DomainResult MarkAsPending()
    {
        if (DeletedAt.HasValue)
            return "Não é possível marcar uma nota fiscal excluída como pendente.";

        if (Status == InvoiceStatus.Closed || Status == InvoiceStatus.Canceled)
            return "Não é possível marcar uma nota fiscal como pendente que esteja Cancelada ou Fechada.";

        Status = InvoiceStatus.Pending;
        UpdatedAt = DateTime.UtcNow;

        return DomainResult.Success();
    }

    public DomainResult MarkAsRejected(string reason)
    {
        if (DeletedAt.HasValue)
            return "Não é possível marcar uma nota fiscal excluída como inconsistente.";

        if (Status != InvoiceStatus.Pending)
            return "Não é possível marcar uma nota fiscal como inconsistente que não esteja Pendente.";

        Status = InvoiceStatus.Rejected;
        ReasonRejected = reason;
        UpdatedAt = DateTime.UtcNow;

        return DomainResult.Success();
    }

    public DomainResult Print()
    {
        if (DeletedAt.HasValue)
            return "Não é possível imprimir uma nota fiscal excluída.";

        if (Status == InvoiceStatus.Closed)
            return "Não é possível imprimir uma nota fiscal fechada.";

        if (Status == InvoiceStatus.Canceled)
            return "Não é possível imprimir uma nota fiscal cancelada.";

        if (Status == InvoiceStatus.Rejected)
            return "Não é possível imprimir uma nota fiscal rejeitada.";

        Status = InvoiceStatus.Closed;
        PrintedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        return DomainResult.Success();
    }

    public void Delete()
    {
        if (DeletedAt.HasValue) return;

        UpdatedAt = DateTime.UtcNow;
        DeletedAt = DateTime.UtcNow;
    }
}
