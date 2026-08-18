using Application.Common.Interfaces;
using Application.Features.Products.Integration.Consumers.Events;
using Application.Features.Products.Integration.Producers.Events;
using Domain.Entities;
using Domain.Enums;
using Domain.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Products.Integration.Consumers.DeadLetter;

public sealed class InvoiceCreatedDeadLetterHandler(
    IUnitWork unitWork,
    ILogger<InvoiceCreatedDeadLetterHandler> logger) : IDeadLetterHandler<InvoiceCreatedEvent>
{
    public async Task CompensateAsync(InvoiceCreatedEvent @event, string failureReason, CancellationToken cancellationToken)
    {
        logger.LogError("DEAD LETTER: Falha permanente ao processar reserva da fatura {InvoiceId} ({Reason}). Executando compensação...", @event.InvoiceId, failureReason);

        var products = await unitWork.AsQueryable<Product>()
            .Include(p => p.Reservations)
            .Where(p => p.Reservations.Any(r => r.InvoiceId == @event.InvoiceId && r.Status == ReservationStatus.Pending))
            .ToListAsync(cancellationToken);

        foreach (var product in products)
        {
            product.CancelReservation(@event.InvoiceId);
        }

        var failEvent = new InventoryReservationFailedEvent(@event.InvoiceId, $"Dead Letter: Falha crítica permanente no inventário ({failureReason}).");
        await unitWork.AddAsync(OutboxMessage.Create(failEvent), cancellationToken);
        await unitWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Compensação de Dead Letter concluída para fatura {InvoiceId}.", @event.InvoiceId);
    }
}
