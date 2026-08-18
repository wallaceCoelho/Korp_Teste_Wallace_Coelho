#pragma warning disable CA1873

using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Application.Features.Invoices.Integration.Producers.Events;

namespace Application.Features.Invoices.Integration.Consumers;

public sealed class InvoiceCreatedDeadLetterHandler(
    IUnitWork unitWork,
    ILogger<InvoiceCreatedDeadLetterHandler> logger) : IDeadLetterHandler<InvoiceCreatedEvent>
{
    public async Task CompensateAsync(InvoiceCreatedEvent @event, string failureReason, CancellationToken cancellationToken)
    {
        var invoice = await unitWork.AsQueryable<Invoice>()
            .FirstOrDefaultAsync(i => i.Id == @event.InvoiceId, cancellationToken);

        if (invoice is null)
        {
            logger.LogWarning("Fatura {InvoiceId} não encontrada para compensação.", @event.InvoiceId);
            return;
        }

        if (invoice.Status == InvoiceStatus.Pending)
        {
            invoice.MarkAsRejected($"Falha definitiva na mensageria ao publicar evento de criação: {failureReason}");
            await unitWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Fatura {InvoiceId} atualizada para FALHA/REJEITADA devido a Dead Letter.", invoice.Id);
        }
    }
}
