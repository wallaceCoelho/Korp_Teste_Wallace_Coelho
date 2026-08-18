using Domain.Messaging;

namespace Application.Features.Invoices.Integration.Producers.Events;

public sealed record InvoicePrintConfirmedEvent(Guid InvoiceId) : IIntegrationEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
