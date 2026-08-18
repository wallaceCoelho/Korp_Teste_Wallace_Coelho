using Domain.Messaging;

namespace Application.Features.Products.Integration.Consumers.Events;

public sealed record InvoiceCreatedEvent(
    Guid InvoiceId,
    long IncrementalNumber,
    List<InvoiceItemDto> Items
) : IIntegrationEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

public sealed record InvoiceItemDto(Guid ProductId, string Code, int Quantity, decimal UnitPrice);
