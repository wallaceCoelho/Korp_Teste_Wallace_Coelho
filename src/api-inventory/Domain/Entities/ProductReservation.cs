using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public sealed class ProductReservation
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public Product Product { get; private set; } = null!;

    public Guid InvoiceId { get; private set; }
    public int Quantity { get; private set; }
    public ReservationStatus Status { get; private set; }

    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; private set; }

    private ProductReservation() { }

    public static DomainResult<ProductReservation> Create(
        Guid productId,
        Guid invoiceId,
        int quantity,
        TimeSpan duration)
    {
        if (productId == Guid.Empty)
            return "ID de produto inválido.";

        if (invoiceId == Guid.Empty)
            return "ID da fatura é obrigatório para rastreabilidade.";

        if (quantity <= 0)
            return "A quantidade a ser reservada deve ser maior que zero.";

        return DomainResult<ProductReservation>.Success(new ProductReservation
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            InvoiceId = invoiceId,
            Quantity = quantity,
            Status = ReservationStatus.Pending,
            ExpiresAt = DateTime.UtcNow.Add(duration),
            CreatedAt = DateTime.UtcNow
        });
    }

    public DomainResult Confirm()
    {
        if (Status != ReservationStatus.Pending)
            return $"Não é possível confirmar uma reserva com status '{Status}'.";

        Status = ReservationStatus.Confirmed;
        UpdatedAt = DateTime.UtcNow;
        return DomainResult.Success();
    }

    public DomainResult Cancel()
    {
        if (Status != ReservationStatus.Pending)
            return $"Não é possível cancelar uma reserva com status '{Status}'.";

        Status = ReservationStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
        return DomainResult.Success();
    }

    public DomainResult MarkAsExpired()
    {
        if (Status != ReservationStatus.Pending)
            return $"Apenas reservas pendentes podem ser expiradas.";

        Status = ReservationStatus.Expired;
        UpdatedAt = DateTime.UtcNow;
        return DomainResult.Success();
    }
}
