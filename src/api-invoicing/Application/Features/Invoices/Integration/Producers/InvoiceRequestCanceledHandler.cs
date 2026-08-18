using Application.Features.Invoices.Integration.Producers.Events;
using Domain.Messaging;

namespace Application.Features.Invoices.Integration.Producers;

public class InvoiceRequestCanceledHandler : IProducerConfig<InvoiceCanceledEvent>
{
    public static void Configure(ProducerOptions options)
    {
        options.Exchange = MessagingConstants.Exchanges.Events;
        options.RoutingKey = MessagingConstants.RoutingKeys.InvoiceCanceled;
        options.MonitoredQueue = MessagingConstants.Queues.InventoryReleaseQueue;
        options.MaxQueueCapacity = 5000;
    }
}
