using Application.Common.Interfaces;
using Application.Features.Products.Integration.Consumers.Events;
using Application.Features.Products.Integration.Producers.Events;
using Domain.Entities;
using Domain.Enums;
using Domain.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Products.Integration.Consumers;

public sealed class InventoryReserveStockConsumerHandler(
    IUnitWork unitWork,
    ILogger<InventoryReserveStockConsumerHandler> logger) :
    IConsumerConfig,
    IIntegrationEventHandler<InvoiceCreatedEvent>
{
    public static void Configure(ConsumerOptions options)
    {
        options.QueueName = MessagingConstants.Queues.InventoryReserveQueue;
        options.Exchange = MessagingConstants.Exchanges.Events;
        options.RoutingKeys = [MessagingConstants.RoutingKeys.InvoiceCreated];
        options.PrefetchCount = 20;
    }

    public async Task HandleAsync(InvoiceCreatedEvent @event, CancellationToken cancellationToken)
    {
        logger.LogInformation("Processando reserva de estoque para fatura {InvoiceId} com {ItemCount} itens.", @event.InvoiceId, @event.Items?.Count ?? 0);

        if (@event.Items is null || @event.Items.Count == 0)
        {
            await EmitReservationFailedAsync(@event.InvoiceId, "A lista de itens da fatura está vazia.", cancellationToken);
            return;
        }

        var existingPendingReservations = await unitWork.AsQueryable<ProductReservation>()
            .Where(r => r.InvoiceId == @event.InvoiceId && r.Status == ReservationStatus.Pending)
            .ToListAsync(cancellationToken);

        foreach (var oldRes in existingPendingReservations)
        {
            oldRes.Cancel();
        }

        var productIds = @event.Items
            .Select(i => i.ProductId)
            .Distinct()
            .OrderBy(id => id)
            .ToList();

        var products = await unitWork.AsQueryable<Product>()
            .Include(p => p.Reservations)
            .Where(p => productIds.Contains(p.Id))
            .OrderBy(p => p.Id)
            .ToListAsync(cancellationToken);

        var missingProductIds = productIds.Except(products.Select(p => p.Id)).ToList();
        if (missingProductIds.Count != 0)
        {
            var missingMsg = $"Produto(s) não encontrado(s) no inventário: {string.Join(", ", missingProductIds)}";
            logger.LogWarning("Falha na reserva da fatura {InvoiceId}: {Reason}", @event.InvoiceId, missingMsg);
            await EmitReservationFailedAsync(@event.InvoiceId, missingMsg, cancellationToken);
            return;
        }

        var reservationsToInsert = new List<ProductReservation>();

        foreach (var item in @event.Items)
        {
            var product = products.First(p => p.Id == item.ProductId);

            if (product.DeletedAt.HasValue)
            {
                var error = $"Não é possível reservar estoque do produto '{product.Code}' pois ele está excluído.";
                logger.LogWarning("Falha na reserva da fatura {InvoiceId}: {Reason}", @event.InvoiceId, error);
                await EmitReservationFailedAsync(@event.InvoiceId, error, cancellationToken);
                return;
            }

            if (product.AvailableStockQuantity < item.Quantity)
            {
                var error = $"Estoque insuficiente para o produto '{product.Code}'. Disponível: {product.AvailableStockQuantity}, Solicitado: {item.Quantity}.";
                logger.LogWarning("Falha ao reservar estoque do produto {ProductCode} ({ProductId}) para fatura {InvoiceId}: {Error}",
                    product.Code, product.Id, @event.InvoiceId, error);

                await EmitReservationFailedAsync(@event.InvoiceId, error, cancellationToken);
                return;
            }

            var reservationResult = ProductReservation.Create(product.Id, @event.InvoiceId, item.Quantity, TimeSpan.FromMinutes(30));
            if (!reservationResult.IsSuccess)
            {
                logger.LogWarning("Falha ao criar reserva para produto {ProductCode} ({ProductId}): {Error}",
                    product.Code, product.Id, reservationResult.Error);

                await EmitReservationFailedAsync(@event.InvoiceId, reservationResult.Error ?? "Erro ao criar reserva.", cancellationToken);
                return;
            }

            reservationsToInsert.Add(reservationResult.Value!);
        }

        foreach (var reservation in reservationsToInsert)
        {
            await unitWork.AddAsync(reservation, cancellationToken);
        }

        var successEvent = new InventoryReservedEvent(@event.InvoiceId);
        await unitWork.AddAsync(OutboxMessage.Create(successEvent), cancellationToken);
        await unitWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Estoque reservado com sucesso para a fatura {InvoiceId}.", @event.InvoiceId);
    }

    private async Task EmitReservationFailedAsync(Guid invoiceId, string reason, CancellationToken cancellationToken)
    {
        var failEvent = new InventoryReservationFailedEvent(invoiceId, reason);
        await unitWork.AddAsync(OutboxMessage.Create(failEvent), cancellationToken);
        await unitWork.SaveChangesAsync(cancellationToken);
    }
}
