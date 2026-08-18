#pragma warning disable CA1873

using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Features.Invoices.Integration.Producers.Events;
using Domain.Entities;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Invoices.CancelInvoice;

public sealed class CancelInvoiceHandler(
    IUnitWork unitWork,
    ILogger<CancelInvoiceHandler> logger) : IRequestHandler<CancelInvoiceCommand, CommandResult<Guid>>
{
    public async ValueTask<CommandResult<Guid>> Handle(CancelInvoiceCommand request, CancellationToken cancellationToken)
    {
        var invoice = await unitWork.AsQueryable<Invoice>()
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken);

        if (invoice is null)
            return new ApiError(ErrorType.NotFound, $"Fatura {request.InvoiceId} não encontrada.");
        
        var cancelResult = invoice.CanCancel();
        if (!cancelResult.IsSuccess)
            return new ApiError(ErrorType.BadRequest, cancelResult.Error!);

        logger.LogInformation("Fatura {InvoiceId} solicitada para cancelamento.", request.InvoiceId);

        return await unitWork.ExecuteInTransactionAsync(async ct =>
        {
            invoice.MarkAsPending();
            await unitWork.SaveChangesAsync(ct);

            var cancelEvent = new InvoiceCanceledEvent(InvoiceId: invoice.Id);
            await unitWork.AddAsync(OutboxMessage.Create(cancelEvent), ct);

            return invoice.Id;
        }, cancellationToken: cancellationToken);
    }
}
