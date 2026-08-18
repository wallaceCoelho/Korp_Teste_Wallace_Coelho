using Application.Features.Invoices.GetInvoices;
using Mediator;

namespace Application.Features.Invoices.StreamInvoiceEvents;

public sealed record StreamInvoiceEventsQuery(Guid Id) : IStreamRequest<InvoiceResponse>;
