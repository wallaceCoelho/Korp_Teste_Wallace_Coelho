using Domain.Messaging;

namespace Application.Features.Products.Integration.Producers.Events;

public sealed record InventoryReservationFailedEvent(Guid InvoiceId, string Reason) : IIntegrationEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
