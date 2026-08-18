using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Application.Features.Invoices.Integration.Consumers.Events;

namespace Application.Features.Invoices.Integration.Consumers;

public sealed class InventoryConfirmedDeadLetterHandler(
    IUnitWork unitWork,
    ILogger<InventoryConfirmedDeadLetterHandler> logger) : IDeadLetterHandler<InventoryConfirmedEvent>
{
    public async Task CompensateAsync(InventoryConfirmedEvent @event, string failureReason, CancellationToken cancellationToken)
    {
        var invoice = await unitWork.AsQueryable<Invoice>()
            .FirstOrDefaultAsync(i => i.Id == @event.InvoiceId, cancellationToken);

        if (invoice is not null && invoice.Status == InvoiceStatus.Open)
        {
            invoice.MarkAsRejected($"Falha no processamento interno da fatura: {failureReason}");
            await unitWork.SaveChangesAsync(cancellationToken);
        }

        logger.LogWarning("Compensação executada para Fatura {InvoiceId}.", @event.InvoiceId);
    }
}
