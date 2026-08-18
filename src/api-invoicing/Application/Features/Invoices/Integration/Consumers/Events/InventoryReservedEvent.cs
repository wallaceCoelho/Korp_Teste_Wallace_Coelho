using Domain.Messaging;

namespace Application.Features.Invoices.Integration.Consumers.Events;

public sealed record InventoryReservedEvent(Guid InvoiceId) : IIntegrationEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
