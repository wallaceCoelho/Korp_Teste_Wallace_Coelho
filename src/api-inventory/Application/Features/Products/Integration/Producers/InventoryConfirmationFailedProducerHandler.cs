using Application.Features.Products.Integration.Producers.Events;
using Domain.Messaging;

namespace Application.Features.Products.Integration.Producers;

public sealed class InventoryConfirmationFailedProducerHandler : IProducerConfig<InventoryConfirmationFailedEvent>
{
    public static void Configure(ProducerOptions options)
    {
        options.Exchange = MessagingConstants.Exchanges.Events;
        options.RoutingKey = MessagingConstants.RoutingKeys.InventoryConfirmationFailed;
        options.MonitoredQueue = MessagingConstants.Queues.InvoiceStatusQueue;
        options.MaxQueueCapacity = 5000;
    }
}
