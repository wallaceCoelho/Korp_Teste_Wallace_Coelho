using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Features.Invoices.CreateInvoice;
using Application.Features.Invoices.Integration.Producers.Events;
using Domain.Entities;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Invoices.UpdateInvoice;

public sealed class UpdateInvoiceHandler(IUnitWork unitWork) : IRequestHandler<UpdateInvoiceCommand, CommandResult<Guid>>
{
    public async ValueTask<CommandResult<Guid>> Handle(UpdateInvoiceCommand request, CancellationToken cancellationToken)
    {
        if (request.Items is null || request.Items.Count == 0)
            return new ApiError(ErrorType.BadRequest, "A nota fiscal deve conter pelo menos um produto.");

        var invoice = await unitWork.AsQueryable<Invoice>()
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken);

        if (invoice is null)
            return new ApiError(ErrorType.NotFound, "Nota fiscal não encontrada.");

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
            var oldItems = invoice.Items.ToList();
            foreach (var oldItem in oldItems)
            {
                unitWork.Delete(oldItem);
            }

            var updateResult = invoice.UpdateItems(domainItems);
            if (!updateResult.IsSuccess)
                return new ApiError(ErrorType.BadRequest, updateResult.Error!);

            foreach (var newItem in domainItems)
            {
                await unitWork.AddAsync(newItem, ct);
            }

            await unitWork.SaveChangesAsync(ct);

            var integrationEvent = new InvoiceCreatedEvent(
                InvoiceId: invoice.Id,
                IncrementalNumber: invoice.Number,
                Items: [.. domainItems.Select(i => new InvoiceItemDto(
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
