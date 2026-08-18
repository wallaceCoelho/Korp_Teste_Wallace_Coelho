using System.Runtime.CompilerServices;
using Application.Common.Interfaces;
using Application.Features.Invoices.GetInvoices;
using Domain.Entities;
using Domain.Enums;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Invoices.StreamInvoiceEvents;

public sealed class StreamInvoiceEventsHandler(IUnitWork unitWork) 
    : IStreamRequestHandler<StreamInvoiceEventsQuery, InvoiceResponse>
{
    public async IAsyncEnumerable<InvoiceResponse> Handle(
        StreamInvoiceEventsQuery request, 
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var invoice = await unitWork.AsQueryable<Invoice>()
                .AsNoTracking()
                .Where(i => i.Id == request.Id)
                .Select(i => new InvoiceResponse(
                    i.Id,
                    i.Number,
                    i.Status,
                    i.Status.ToString(),
                    i.ReasonRejected,
                    i.TotalAmount,
                    i.CreatedAt,
                    i.UpdatedAt,
                    i.PrintedAt,
                    i.Items.Select(item => new InvoiceItemResponse(
                        item.Id,
                        item.ProductId,
                        item.ProductCode,
                        item.ProductDescription,
                        item.Quantity,
                        item.UnitPrice,
                        item.TotalPrice
                    )).ToList()
                ))
                .FirstOrDefaultAsync(cancellationToken);

            if (invoice is null)
            {
                yield break;
            }

            yield return invoice;

            if (invoice.Status != InvoiceStatus.Pending)
            {
                yield break;
            }

            await Task.Delay(1000, cancellationToken);
        }
    }
}
