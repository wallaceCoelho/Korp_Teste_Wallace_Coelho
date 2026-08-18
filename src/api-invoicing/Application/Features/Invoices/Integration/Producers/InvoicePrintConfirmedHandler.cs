using Application.Features.Invoices.Integration.Producers.Events;
using Domain.Messaging;

namespace Application.Features.Invoices.Integration.Producers;

public class InvoicePrintConfirmedHandler : IProducerConfig<InvoicePrintConfirmedEvent>
{
    public static void Configure(ProducerOptions options)
    {
        options.Exchange = MessagingConstants.Exchanges.Events;
        options.RoutingKey = MessagingConstants.RoutingKeys.InvoicePrintConfirmed;
        options.MonitoredQueue = MessagingConstants.Queues.InventoryConfirmQueue;
        options.MaxQueueCapacity = 5000;
    }
}
