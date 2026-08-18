using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Features.Invoices.Integration.Producers.Events;
using Domain.Entities;
using Mediator;

namespace Application.Features.Invoices.CreateInvoice;

public sealed class CreateInvoiceHandler(IUnitWork unitWork) : IRequestHandler<CreateInvoiceCommand, CommandResult<Guid>>
{
    public async ValueTask<CommandResult<Guid>> Handle(CreateInvoiceCommand request, CancellationToken cancellationToken)
    {
        if (request.Items is null || request.Items.Count == 0)
            return new ApiError(ErrorType.BadRequest, "A nota fiscal deve conter pelo menos um produto.");

        var domainItems = new List<InvoiceItem>();

        foreach (var itemReq in request.Items)
        {
            var itemResult = InvoiceItem.Create(
                itemReq.ProductId,
                itemReq.ProductCode,
                itemReq.ProductDescription,
                itemReq.Quantity,
                itemReq.UnitPrice
            );

            if (!itemResult.IsSuccess || itemResult.Value is null)
            {
                return new ApiError(ErrorType.BadRequest, itemResult.Error!);
            }

            domainItems.Add(itemResult.Value);
        }

        return await unitWork.ExecuteInTransactionAsync<CommandResult<Guid>>(async ct =>
        {
            var invoiceResult = Invoice.Create(domainItems);
            if (!invoiceResult.IsSuccess || invoiceResult.Value is null)
                return new ApiError(ErrorType.BadRequest, invoiceResult.Error!);

            var invoice = invoiceResult.Value!;

            await unitWork.AddAsync(invoice, ct);
            await unitWork.SaveChangesAsync(ct);

            var integrationEvent = new InvoiceCreatedEvent(
                InvoiceId: invoice.Id,
                IncrementalNumber: invoice.Number,
                Items: [.. invoice.Items.Select(i => new InvoiceItemDto(
                    i.ProductId, 
                    i.ProductCode, 
                    i.Quantity, 
                    i.UnitPrice))]
            );

            var outboxMessage = OutboxMessage.Create(integrationEvent);
            await unitWork.AddAsync(outboxMessage, ct);

            return invoice.Id;
        }, cancellationToken: cancellationToken);
    }
}
