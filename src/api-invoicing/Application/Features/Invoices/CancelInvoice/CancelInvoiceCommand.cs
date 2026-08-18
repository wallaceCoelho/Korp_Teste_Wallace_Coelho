using Application.Common.Results;
using Mediator;

namespace Application.Features.Invoices.CancelInvoice;

public sealed record CancelInvoiceCommand(Guid InvoiceId) : IRequest<CommandResult<Guid>>;
