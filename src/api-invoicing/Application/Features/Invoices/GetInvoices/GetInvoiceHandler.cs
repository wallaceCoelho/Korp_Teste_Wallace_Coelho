using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Domain.Entities;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Invoices.GetInvoices;

public sealed class GetInvoiceHandler(IUnitWork unitWork) : IRequestHandler<GetInvoiceQuery, QueryResult<InvoiceResponse>>
{
    public async ValueTask<QueryResult<InvoiceResponse>> Handle(GetInvoiceQuery request, CancellationToken cancellationToken)
    {
        var invoice = await unitWork.AsQueryable<Invoice>()
            .AsNoTracking()
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
            .FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken);

        return invoice is null 
            ? new ApiError(ErrorType.NotFound, "Nota fiscal não encontrada.") 
            : invoice;
    }
}
