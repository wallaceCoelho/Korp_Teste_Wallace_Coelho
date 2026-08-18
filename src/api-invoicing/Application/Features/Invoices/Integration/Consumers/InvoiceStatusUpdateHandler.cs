using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Application.Features.Invoices.Integration.Consumers.Events;

namespace Application.Features.Invoices.Integration.Consumers;

public sealed class InvoiceStatusUpdateHandler(
    IUnitWork unitWork,
    ILogger<InvoiceStatusUpdateHandler> logger) :
    IConsumerConfig,
    IIntegrationEventHandler<InventoryReservedEvent>,
    IIntegrationEventHandler<InventoryReservationFailedEvent>,
    IIntegrationEventHandler<InventoryConfirmedEvent>,
    IIntegrationEventHandler<InventoryConfirmationFailedEvent>,
    IIntegrationEventHandler<InventoryReservedCanceledEvent>
{
    public static void Configure(ConsumerOptions options)
    {
        options.QueueName = MessagingConstants.Queues.InvoiceStatusQueue;
        options.Exchange = MessagingConstants.Exchanges.Events;
        options.RoutingKeys =
        [
            MessagingConstants.RoutingKeys.InventoryReserved,
            MessagingConstants.RoutingKeys.InventoryConfirmed,
            MessagingConstants.RoutingKeys.InventoryReservedCanceled,
            MessagingConstants.RoutingKeys.InventoryReservationFailed,
            MessagingConstants.RoutingKeys.InventoryConfirmationFailed
        ];
        options.PrefetchCount = 20;
    }

    public async Task HandleAsync(InventoryReservedEvent @event, CancellationToken cancellationToken)
    {
        var invoice = await unitWork.AsQueryable<Invoice>()
            .FirstOrDefaultAsync(i => i.Id == @event.InvoiceId, cancellationToken);

        if (invoice is null)
        {
            logger.LogError("Fatura {InvoiceId} não encontrada para aprovação.", @event.InvoiceId);
            return;
        }

        var openResult = invoice.Open();
        if (!openResult.IsSuccess)
        {
            logger.LogError("Não foi possível abrir a fatura {InvoiceId}. Motivo: {Reason}", invoice.Id, openResult.Error);
            return;
        }

        await unitWork.SaveChangesAsync(cancellationToken);
    }

    public async Task HandleAsync(InventoryConfirmedEvent @event, CancellationToken cancellationToken)
    {
        var invoice = await unitWork.AsQueryable<Invoice>()
            .FirstOrDefaultAsync(i => i.Id == @event.InvoiceId, cancellationToken);

        if (invoice is null)
        {
            logger.LogError("Fatura {InvoiceId} não encontrada para aprovação.", @event.InvoiceId);
            return;
        }

        var printResult = invoice.Print();
        if (!printResult.IsSuccess)
        {
            logger.LogError("Não foi possível imprimir a fatura {InvoiceId}. Motivo: {Reason}", invoice.Id, printResult.Error);
            return;
        }

        await unitWork.SaveChangesAsync(cancellationToken);
    }

    public async Task HandleAsync(InventoryReservedCanceledEvent @event, CancellationToken cancellationToken)
    {
        var invoice = await unitWork.AsQueryable<Invoice>()
            .FirstOrDefaultAsync(i => i.Id == @event.InvoiceId, cancellationToken);

        if (invoice is null)
        {
            logger.LogError("Fatura {InvoiceId} não encontrada para cancelamento.", @event.InvoiceId);
            return;
        }

        var cancelResult = invoice.Cancel();
        if (!cancelResult.IsSuccess)
        {
            logger.LogError("Não foi possível cancelar a fatura {InvoiceId}. Motivo: {Reason}", invoice.Id, cancelResult.Error);
            return;
        }

        await unitWork.SaveChangesAsync(cancellationToken);
    }

    public async Task HandleAsync(InventoryReservationFailedEvent @event, CancellationToken cancellationToken)
    {
        var invoice = await unitWork.AsQueryable<Invoice>()
            .FirstOrDefaultAsync(i => i.Id == @event.InvoiceId, cancellationToken);

        if (invoice is null)
        {
            logger.LogError("Fatura {InvoiceId} não encontrada para rejeição.", @event.InvoiceId);
            return;
        }

        invoice.MarkAsRejected(@event.Reason);
        await unitWork.SaveChangesAsync(cancellationToken);

        logger.LogWarning("Fatura {InvoiceId} rejeitada. Motivo: {Reason}", invoice.Id, @event.Reason);
    }

    public async Task HandleAsync(InventoryConfirmationFailedEvent @event, CancellationToken cancellationToken)
    {
        var invoice = await unitWork.AsQueryable<Invoice>()
            .FirstOrDefaultAsync(i => i.Id == @event.InvoiceId, cancellationToken);

        if (invoice is null)
        {
            logger.LogError("Fatura {InvoiceId} não encontrada para rejeição.", @event.InvoiceId);
            return;
        }

        invoice.MarkAsRejected(@event.Reason);
        await unitWork.SaveChangesAsync(cancellationToken);

        logger.LogWarning("Fatura {InvoiceId} rejeitada. Motivo: {Reason}", invoice.Id, @event.Reason);
    }
}
