using Application.Common.Results;
using Mediator;

namespace Application.Features.Invoices.CreateInvoice;

public sealed record CreateInvoiceItemCommand(
    Guid ProductId,
    string ProductCode,
    string ProductDescription,
    int Quantity,
    decimal UnitPrice
);

public sealed record CreateInvoiceCommand(
    List<CreateInvoiceItemCommand> Items
) : IRequest<CommandResult<Guid>>;
