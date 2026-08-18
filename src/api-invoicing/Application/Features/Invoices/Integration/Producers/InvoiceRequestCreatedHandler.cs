using Domain.Messaging;
using Application.Features.Invoices.Integration.Producers.Events;

namespace Application.Features.Invoices.Integration.Producers;

public sealed class InvoiceRequestCreatedHandler : IProducerConfig<InvoiceCreatedEvent>
{
    public static void Configure(ProducerOptions options)
    {
        options.Exchange = MessagingConstants.Exchanges.Events;
        options.RoutingKey = MessagingConstants.RoutingKeys.InvoiceCreated;
        options.MonitoredQueue = MessagingConstants.Queues.InventoryReserveQueue;
        options.MaxQueueCapacity = 5000;
    }
}
