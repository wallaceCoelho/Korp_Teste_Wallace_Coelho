using Domain.Messaging;

namespace Application.Features.Invoices.Integration.Consumers.Events;

public sealed record InventoryReservedCanceledEvent(Guid InvoiceId) : IIntegrationEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
