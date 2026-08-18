using Application.Common.Results;
using Application.Features.Invoices.CreateInvoice;
using Mediator;

namespace Application.Features.Invoices.UpdateInvoice;

public sealed record UpdateInvoiceCommand(
    Guid Id,
    List<CreateInvoiceItemCommand> Items
) : IRequest<CommandResult<Guid>>;
