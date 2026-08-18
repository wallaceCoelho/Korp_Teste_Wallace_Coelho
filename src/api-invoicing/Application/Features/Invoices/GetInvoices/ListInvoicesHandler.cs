using Application.Common.Interfaces;
using Application.Common.Results;
using Domain.Entities;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Invoices.GetInvoices;

public sealed class ListInvoicesHandler(IUnitWork unitWork) : IRequestHandler<ListInvoicesQuery, QueryResult<ListInvoicesResponse>>
{
    public async ValueTask<QueryResult<ListInvoicesResponse>> Handle(ListInvoicesQuery request, CancellationToken cancellationToken)
    {
        var query = unitWork.AsQueryable<Invoice>().AsNoTracking();

        string? searchPattern = request.Query.GetSearchPattern();
        if (!string.IsNullOrEmpty(searchPattern))
        {
            if (long.TryParse(request.Query.Search?.Trim(), out long parsedNumber))
            {
                query = query.Where(i => i.Number == parsedNumber);
            }
            else
            {
                query = query.Where(i => i.Items.Any(item =>
                    EF.Functions.Like(item.ProductCode, searchPattern) ||
                    EF.Functions.Like(item.ProductDescription, searchPattern)));
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);

        if (request.Query.IsPagination())
        {
            query = query.Skip(request.Query.GetSkip())
                         .Take(request.Query.GetPageSize() ?? 100);
        }

        var invoices = await query
            .OrderByDescending(i => i.Number)
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
            )).ToListAsync(cancellationToken);

        return new ListInvoicesResponse(invoices, totalCount);
    }
}
