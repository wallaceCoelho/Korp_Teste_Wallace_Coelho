using Application.Common.Interfaces;
using Application.Features.Products.Integration.Consumers.Events;
using Application.Features.Products.Integration.Producers.Events;
using Domain.Entities;
using Domain.Messaging;
using Microsoft.Extensions.Logging;

namespace Application.Features.Products.Integration.Consumers.DeadLetter;

public sealed class InvoicePrintConfirmedDeadLetterHandler(
    IUnitWork unitWork,
    ILogger<InvoicePrintConfirmedDeadLetterHandler> logger) : IDeadLetterHandler<InvoicePrintConfirmedEvent>
{
    public async Task CompensateAsync(InvoicePrintConfirmedEvent @event, string failureReason, CancellationToken cancellationToken)
    {
        logger.LogError("DEAD LETTER: Falha permanente ao confirmar baixa de estoque para fatura {InvoiceId} ({Reason}). Executando compensação...", @event.InvoiceId, failureReason);

        var failEvent = new InventoryConfirmationFailedEvent(@event.InvoiceId, $"Dead Letter: Falha crítica permanente no inventário ({failureReason}).");
        await unitWork.AddAsync(OutboxMessage.Create(failEvent), cancellationToken);
        await unitWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Compensação de Dead Letter concluída para confirmação da fatura {InvoiceId}.", @event.InvoiceId);
    }
}
