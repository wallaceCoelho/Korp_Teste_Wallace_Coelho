using Application.Common.Interfaces;
using Application.Features.Products.Integration.Consumers.Events;
using Application.Features.Products.Integration.Producers.Events;
using Domain.Entities;
using Domain.Enums;
using Domain.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace Application.Features.Products.Integration.Consumers;

public sealed class InventoryConfirmStockConsumerHandler(
    IUnitWork unitWork,
    ILogger<InventoryConfirmStockConsumerHandler> logger) :
    IConsumerConfig,
    IIntegrationEventHandler<InvoicePrintConfirmedEvent>
{
    private readonly ResiliencePipeline _resiliencePipeline = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            ShouldHandle = new PredicateBuilder().Handle<DbUpdateConcurrencyException>(),
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(50),
            BackoffType = DelayBackoffType.Exponential,
            OnRetry = args =>
            {
                logger.LogWarning(args.Outcome.Exception, "Concorrência detectada ao confirmar estoque. Tentativa {AttemptNumber}.", args.AttemptNumber);
                return ValueTask.CompletedTask;
            }
        })
        .Build();

    public static void Configure(ConsumerOptions options)
    {
        options.QueueName = MessagingConstants.Queues.InventoryConfirmQueue;
        options.Exchange = MessagingConstants.Exchanges.Events;
        options.RoutingKeys = [MessagingConstants.RoutingKeys.InvoicePrintConfirmed];
        options.PrefetchCount = 20;
    }

    public async Task HandleAsync(InvoicePrintConfirmedEvent @event, CancellationToken cancellationToken)
    {
        logger.LogInformation("Processando confirmação e baixa de estoque para a fatura {InvoiceId}.", @event.InvoiceId);

        await _resiliencePipeline.ExecuteAsync(async ct =>
        {
            var products = await unitWork.AsQueryable<Product>()
                .Include(p => p.Reservations)
                .Where(p => p.Reservations.Any(r => r.InvoiceId == @event.InvoiceId && r.Status == ReservationStatus.Pending))
                .OrderBy(p => p.Id)
                .ToListAsync(ct);

            if (products.Count == 0)
            {
                logger.LogWarning("Nenhuma reserva pendente encontrada para a fatura {InvoiceId}.", @event.InvoiceId);
                await EmitConfirmationFailedAsync(@event.InvoiceId, "Nenhuma reserva pendente encontrada para a fatura informada.", ct);
                return;
            }

            foreach (var product in products)
            {
                var reservation = product.Reservations.FirstOrDefault(r => r.InvoiceId == @event.InvoiceId && r.Status == ReservationStatus.Pending);
                if (reservation is null)
                {
                    await EmitConfirmationFailedAsync(@event.InvoiceId, $"Reserva não encontrada para o produto '{product.Code}'.", ct);
                    return;
                }

                if (product.DeletedAt.HasValue)
                {
                    await EmitConfirmationFailedAsync(@event.InvoiceId, $"Não é possível confirmar a fatura pois o produto '{product.Code}' foi excluído.", ct);
                    return;
                }

                if (product.StockQuantity < reservation.Quantity)
                {
                    var error = $"Estoque insuficiente para o produto '{product.Code}'. Disponível: {product.StockQuantity}, Solicitado: {reservation.Quantity}.";
                    logger.LogWarning("Falha ao confirmar baixa de estoque para fatura {InvoiceId}: {Error}", @event.InvoiceId, error);
                    await EmitConfirmationFailedAsync(@event.InvoiceId, error, ct);
                    return;
                }
            }

            foreach (var product in products)
            {
                var result = product.ConfirmReservationAndDeduct(@event.InvoiceId);
                if (!result.IsSuccess)
                {
                    await EmitConfirmationFailedAsync(@event.InvoiceId, result.Error ?? "Erro ao deduzir reserva.", ct);
                    return;
                }
            }

            var successEvent = new InventoryConfirmedEvent(@event.InvoiceId);
            await unitWork.AddAsync(OutboxMessage.Create(successEvent), ct);
            await unitWork.SaveChangesAsync(ct);

            logger.LogInformation("Baixa de estoque confirmada com sucesso para a fatura {InvoiceId}.", @event.InvoiceId);
        }, cancellationToken);
    }

    private async Task EmitConfirmationFailedAsync(Guid invoiceId, string reason, CancellationToken cancellationToken)
    {
        var reservations = await unitWork.AsQueryable<ProductReservation>()
            .Where(r => r.InvoiceId == invoiceId && r.Status == ReservationStatus.Pending)
            .ToListAsync(cancellationToken);

        foreach (var reservation in reservations)
        {
            reservation.Cancel();
        }

        var failEvent = new InventoryConfirmationFailedEvent(invoiceId, reason);
        await unitWork.AddAsync(OutboxMessage.Create(failEvent), cancellationToken);
        await unitWork.SaveChangesAsync(cancellationToken);
    }
}
