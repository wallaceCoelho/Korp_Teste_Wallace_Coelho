using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Features.Invoices.Integration.Producers.Events;
using Domain.Entities;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Invoices.PrintInvoice;

public sealed class PrintInvoiceHandler(IUnitWork unitWork) : IRequestHandler<PrintInvoiceCommand, CommandResult<Guid>>
{
    public async ValueTask<CommandResult<Guid>> Handle(PrintInvoiceCommand request, CancellationToken cancellationToken)
    {
        var invoice = await unitWork.AsQueryable<Invoice>()
            .FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken);

        if (invoice is null)
            return new ApiError(ErrorType.NotFound, "Nota fiscal não encontrada.");

        var canPrintResult = invoice.SeeCanPrint();
        if (!canPrintResult.IsSuccess)
            return new ApiError(ErrorType.Conflict, canPrintResult.Error!);

        return await unitWork.ExecuteInTransactionAsync(async ct =>
        {
            invoice.MarkAsPending();
            await unitWork.SaveChangesAsync(ct);

            var printEvent = new InvoicePrintConfirmedEvent(InvoiceId: invoice.Id);
            await unitWork.AddAsync(OutboxMessage.Create(printEvent), ct);

            return invoice.Id;
        }, cancellationToken: cancellationToken);
    }
}
