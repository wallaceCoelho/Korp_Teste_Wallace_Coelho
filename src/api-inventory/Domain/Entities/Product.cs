using Domain.Common;
using Domain.Enums;
using NpgsqlTypes;

namespace Domain.Entities;

public sealed class Product
{
    public Guid Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int StockQuantity { get; private set; }
    public int? MinStockQuantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    public Guid? CategoryId { get; private set; }
    public Category? Category { get; private set; }

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    public NpgsqlTsVector SearchVector { get; private set; } = null!;

    public uint Version { get; private set; }

    private readonly List<ProductReservation> _reservations = [];
    public IReadOnlyCollection<ProductReservation> Reservations => _reservations.AsReadOnly();

    public int AvailableStockQuantity => Math.Max(0, StockQuantity - _reservations
        .Where(r => r.Status == ReservationStatus.Pending && r.ExpiresAt > DateTime.UtcNow)
        .Sum(r => r.Quantity));

    private Product() { }

    public static DomainResult<Product> Create(
        string code,
        string name,
        int initialStock,
        decimal unitPrice,
        string? description = null,
        int? minStock = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            return "Código é obrigatório.";

        if (string.IsNullOrWhiteSpace(name))
            return "Nome do produto é obrigatório.";

        if (unitPrice < 0)
            return "O preço unitário não pode ser negativo.";

        if (initialStock < 0)
            return "Quantidade inicial em estoque não pode ser negativa.";

        return DomainResult<Product>.Success(new Product
        {
            Id = Guid.NewGuid(),
            Code = code.Trim().ToUpper(),
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            StockQuantity = initialStock,
            MinStockQuantity = minStock,
            UnitPrice = unitPrice,
            CreatedAt = DateTime.UtcNow
        });
    }

    public DomainResult UpdateDetails(
        string newCode,
        string newName,
        decimal newUnitPrice,
        string? newDescription = null,
        int? minStock = null)
    {
        if (DeletedAt.HasValue)
            return "Não é possível atualizar um produto excluído.";

        if (string.IsNullOrWhiteSpace(newCode))
            return "Código é obrigatório.";

        if (string.IsNullOrWhiteSpace(newName))
            return "Nome do produto é obrigatório.";

        if (newUnitPrice < 0)
            return "O preço unitário não pode ser negativo.";

        Code = newCode.Trim().ToUpper();
        Name = newName.Trim();
        Description = string.IsNullOrWhiteSpace(newDescription) ? null : newDescription.Trim();
        UnitPrice = newUnitPrice;
        MinStockQuantity = minStock;
        UpdatedAt = DateTime.UtcNow;

        return DomainResult.Success();
    }

    public DomainResult<ProductReservation> ReserveStock(Guid invoiceId, int quantity, TimeSpan reservationDuration)
    {
        if (DeletedAt.HasValue)
            return "Não é possível reservar estoque de um produto excluído.";

        var existingReservation = _reservations.FirstOrDefault(r => r.InvoiceId == invoiceId && r.Status == ReservationStatus.Pending);
        if (existingReservation is not null)
            return DomainResult<ProductReservation>.Success(existingReservation);

        if (AvailableStockQuantity < quantity)
            return $"Estoque insuficiente para o produto '{Code}'. Disponível: {AvailableStockQuantity}, Solicitado: {quantity}.";

        var reservationResult = ProductReservation.Create(Id, invoiceId, quantity, reservationDuration);
        if (!reservationResult.IsSuccess)
            return reservationResult.Error!;

        _reservations.Add(reservationResult.Value!);

        return reservationResult;
    }

    public DomainResult ConfirmReservationAndDeduct(Guid invoiceId)
    {
        if (DeletedAt.HasValue)
            return "Não é possível atualizar um produto excluído.";

        var reservation = _reservations.FirstOrDefault(r => r.InvoiceId == invoiceId && r.Status == ReservationStatus.Pending);
        if (reservation is null)
            return $"Nenhuma reserva pendente encontrada para a fatura '{invoiceId}'.";

        if (StockQuantity < reservation.Quantity)
            return $"Estoque físico insuficiente para o produto '{Code}'. Disponível: {StockQuantity}, Solicitado: {reservation.Quantity}.";

        var confirmResult = reservation.Confirm();
        if (!confirmResult.IsSuccess)
            return confirmResult.Error!;

        StockQuantity -= reservation.Quantity;
        UpdatedAt = DateTime.UtcNow;

        return DomainResult.Success();
    }

    public DomainResult CancelReservation(Guid invoiceId)
    {
        var reservation = _reservations.FirstOrDefault(r => r.InvoiceId == invoiceId && r.Status == ReservationStatus.Pending);
        if (reservation is null)
            return DomainResult.Success();

        var cancelResult = reservation.Cancel();
        if (!cancelResult.IsSuccess)
            return cancelResult.Error!;

        return DomainResult.Success();
    }

    public DomainResult DeductStock(int quantity)
    {
        if (DeletedAt.HasValue)
            return "Não é possível atualizar um produto excluído.";

        if (quantity <= 0)
            return "A quantidade a deduzir deve ser maior que zero.";

        if (StockQuantity < quantity)
            return $"Estoque insuficiente para o produto '{Code}'. Disponível: {StockQuantity}, Solicitado: {quantity}.";

        // =========================================================================================
        // REGRA DE CONFLITO COM RESERVAS ATIVAS (Deixada comentada para testes de falha na Saga/Invoice):
        // Se a quantidade deduzida for afetar as notas já reservadas, retornar erro de conflito:
        // -----------------------------------------------------------------------------------------
        // var activeReservedQuantity = _reservations
        //     .Where(r => r.Status == ReservationStatus.Pending && r.ExpiresAt > DateTime.UtcNow)
        //     .Sum(r => r.Quantity);
        //
        // if (StockQuantity - quantity < activeReservedQuantity)
        // {
        //     return $"Não é possível deduzir {quantity} unidades pois afetaria as reservas ativas ({activeReservedQuantity} un reservadas). Saldo livre para dedução: {AvailableStockQuantity}.";
        // }
        // =========================================================================================

        StockQuantity = quantity > StockQuantity ? 0 : StockQuantity - quantity;
        UpdatedAt = DateTime.UtcNow;

        return DomainResult.Success();
    }

    public DomainResult AddStock(int quantity)
    {
        if (DeletedAt.HasValue)
            return "Não é possível atualizar um produto excluído.";

        if (quantity <= 0)
            return "A quantidade a adicionar deve ser maior que zero.";

        StockQuantity += quantity;
        UpdatedAt = DateTime.UtcNow;

        return DomainResult.Success();
    }

    public DomainResult ChangeCategory(Guid? newCategoryId)
    {
        if (DeletedAt.HasValue)
            return "Não é possível atualizar um produto excluído.";

        CategoryId = (newCategoryId == null || newCategoryId == Guid.Empty) ? null : newCategoryId;
        UpdatedAt = DateTime.UtcNow;

        return DomainResult.Success();
    }

    public DomainResult RemoveCategory()
    {
        CategoryId = null;
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
