using Application.Common.Queries;
using Application.Common.Results;
using Domain.Enums;
using Mediator;

namespace Application.Features.Invoices.GetInvoices;

public sealed record GetInvoiceQuery(Guid Id) : IRequest<QueryResult<InvoiceResponse>>;
public sealed record ListInvoicesQuery(QueryParams Query) : IRequest<QueryResult<ListInvoicesResponse>>;

public sealed record InvoiceResponse(
    Guid Id,
    long Number,
    InvoiceStatus Status,
    string StatusDescription,
    string? ReasonRejected,
    decimal TotalAmount,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? PrintedAt,
    List<InvoiceItemResponse> Items
);

public sealed record InvoiceItemResponse(
    Guid Id,
    Guid ProductId,
    string ProductCode,
    string ProductDescription,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice
);

public sealed record ListInvoicesResponse(List<InvoiceResponse> Items, long TotalCount);
