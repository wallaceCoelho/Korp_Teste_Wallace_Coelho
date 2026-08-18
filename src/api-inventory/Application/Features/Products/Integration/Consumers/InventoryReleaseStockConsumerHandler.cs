using Application.Common.Interfaces;
using Application.Features.Products.Integration.Consumers.Events;
using Application.Features.Products.Integration.Producers.Events;
using Domain.Entities;
using Domain.Enums;
using Domain.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Products.Integration.Consumers;

public sealed class InventoryReleaseStockConsumerHandler(
    IUnitWork unitWork,
    ILogger<InventoryReleaseStockConsumerHandler> logger) :
    IConsumerConfig,
    IIntegrationEventHandler<InvoiceCanceledEvent>
{
    public static void Configure(ConsumerOptions options)
    {
        options.QueueName = MessagingConstants.Queues.InventoryReleaseQueue;
        options.Exchange = MessagingConstants.Exchanges.Events;
        options.RoutingKeys = [MessagingConstants.RoutingKeys.InvoiceCanceled];
        options.PrefetchCount = 20;
    }

    public async Task HandleAsync(InvoiceCanceledEvent @event, CancellationToken cancellationToken)
    {
        logger.LogInformation("Processando liberação/cancelamento de reserva para a fatura {InvoiceId}.", @event.InvoiceId);

        var reservations = await unitWork.AsQueryable<ProductReservation>()
            .Where(r => r.InvoiceId == @event.InvoiceId && r.Status == ReservationStatus.Pending)
            .ToListAsync(cancellationToken);

        foreach (var reservation in reservations)
        {
            reservation.Cancel();
        }

        var cancelEvent = new InventoryReservedCanceledEvent(@event.InvoiceId);
        await unitWork.AddAsync(OutboxMessage.Create(cancelEvent), cancellationToken);
        await unitWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Reserva de estoque cancelada/liberada para a fatura {InvoiceId} ({Count} reservas canceladas).",
            @event.InvoiceId, reservations.Count);
    }
}
