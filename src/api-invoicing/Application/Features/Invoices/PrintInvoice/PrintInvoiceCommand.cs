using Application.Common.Results;
using Mediator;

namespace Application.Features.Invoices.PrintInvoice;

public sealed record PrintInvoiceCommand(Guid Id) : IRequest<CommandResult<Guid>>;
