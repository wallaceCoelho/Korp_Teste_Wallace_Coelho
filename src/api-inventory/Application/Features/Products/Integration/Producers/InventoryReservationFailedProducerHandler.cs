using Application.Features.Products.Integration.Producers.Events;
using Domain.Messaging;

namespace Application.Features.Products.Integration.Producers;

public sealed class InventoryReservationFailedProducerHandler : IProducerConfig<InventoryReservationFailedEvent>
{
    public static void Configure(ProducerOptions options)
    {
        options.Exchange = MessagingConstants.Exchanges.Events;
        options.RoutingKey = MessagingConstants.RoutingKeys.InventoryReservationFailed;
        options.MonitoredQueue = MessagingConstants.Queues.InvoiceStatusQueue;
        options.MaxQueueCapacity = 5000;
    }
}
