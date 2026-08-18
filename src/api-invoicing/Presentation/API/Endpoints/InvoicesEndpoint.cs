using Application.Common.Queries;
using Application.Features.Invoices.CancelInvoice;
using Application.Features.Invoices.CreateInvoice;
using Application.Features.Invoices.GetInvoices;
using Application.Features.Invoices.PrintInvoice;
using Application.Features.Invoices.StreamInvoiceEvents;
using Mediator;
using Presentation.API.Extensions;

namespace Presentation.API.Endpoints;

public static class InvoicesEndpoint
{
    public static void MapInvoicesEndpoints(this IEndpointRouteBuilder app)
    {
        const string route = "/api/invoices";
        var group = app.MapGroup(route).WithTags("Invoices");

        group.MapPost("/", async (CreateInvoiceCommand command, IMediator mediator) =>
        {
            var result = await mediator.Send(command);
            return result.ToHttpResult((id) => Results.Created($"{route}/{id}", new { id }));
        }).GetCreateInvoiceDocs();

        group.MapPut("/{id:guid}", async (Guid id, List<CreateInvoiceItemCommand> items, IMediator mediator) =>
        {
            var result = await mediator.Send(new Application.Features.Invoices.UpdateInvoice.UpdateInvoiceCommand(id, items));
            return result.ToHttpResult();
        }).GetUpdateInvoiceDocs();

        group.MapPost("/{id:guid}/print", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new PrintInvoiceCommand(id));
            return result.ToHttpResult();
        }).GetPrintInvoiceDocs();

        group.MapPost("/{id:guid}/cancel", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new CancelInvoiceCommand(id));
            return result.ToHttpResult();
        }).GetCancelInvoiceDocs();

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetInvoiceQuery(id));
            return result.ToHttpResult();
        }).GetGetInvoiceByIdDocs();

        group.MapGet("/{id:guid}/events", (Guid id, HttpContext httpContext, IMediator mediator) =>
            httpContext.StreamSSEAsync(mediator.CreateStream(new StreamInvoiceEventsQuery(id))))
        .GetInvoiceEventsDocs();

        group.MapGet("/", async ([AsParameters] QueryParams query, IMediator mediator) =>
        {
            var result = await mediator.Send(new ListInvoicesQuery(query));
            return result.ToHttpResult();
        }).GetListInvoicesDocs();
    }

    #region ENDPOINTS DOCS
    
    private static RouteHandlerBuilder GetCancelInvoiceDocs(this RouteHandlerBuilder route)
    {
        route.WithName("CancelInvoice")
             .WithSummary("Cancela uma nota fiscal")
             .WithDescription("Valida se o status da nota é Aberta, atualiza o status para Cancelada e devolve as quantidades dos produtos no estoque.")
             .Produces<Guid>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status400BadRequest)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status409Conflict)
             .ProducesProblem(StatusCodes.Status500InternalServerError);
        return route;
    }

    private static RouteHandlerBuilder GetCreateInvoiceDocs(this RouteHandlerBuilder route)
    {
        route.WithName("CreateInvoice")
             .WithSummary("Cria uma nova nota fiscal")
             .WithDescription("Emite uma nota fiscal com numeração sequencial atômica, lista de produtos e status inicial Aberta.")
             .Produces<Guid>(StatusCodes.Status201Created)
             .ProducesProblem(StatusCodes.Status400BadRequest)
             .ProducesProblem(StatusCodes.Status500InternalServerError)
             .ProducesProblem(StatusCodes.Status409Conflict)
             .ProducesProblem(StatusCodes.Status500InternalServerError);
        return route;
    }

    private static RouteHandlerBuilder GetUpdateInvoiceDocs(this RouteHandlerBuilder route)
    {
        route.WithName("UpdateInvoice")
             .WithSummary("Atualiza e reenvia uma nota fiscal rejeitada")
             .WithDescription("Permite corrigir itens de uma fatura rejeitada e reenviar para reserva de estoque.")
             .Produces<Guid>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status400BadRequest)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status500InternalServerError);
        return route;
    }

    private static RouteHandlerBuilder GetPrintInvoiceDocs(this RouteHandlerBuilder route)
    {
        route.WithName("PrintInvoice")
             .WithSummary("Processa a impressão da nota fiscal")
             .WithDescription("Valida se o status da nota é Aberta, atualiza o status para Fechada e deduz as quantidades dos produtos no estoque.")
             .Produces<Guid>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status400BadRequest)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status409Conflict)
             .ProducesProblem(StatusCodes.Status500InternalServerError);
        return route;
    }

    private static RouteHandlerBuilder GetGetInvoiceByIdDocs(this RouteHandlerBuilder route)
    {
        route.WithName("GetInvoiceById")
             .WithSummary("Obtém uma nota fiscal pelo ID")
             .WithDescription("Retorna os detalhes completos da nota fiscal, incluindo seus itens e valor total.")
             .Produces<InvoiceResponse>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status500InternalServerError);
        return route;
    }

    private static RouteHandlerBuilder GetInvoiceEventsDocs(this RouteHandlerBuilder route)
    {
        route.WithName("GetInvoiceEvents")
             .WithSummary("Streaming SSE para eventos e mudanças de status da fatura")
             .WithDescription("Abre um stream Server-Sent Events que notifica em tempo real a conclusão da reserva de estoque.")
             .Produces<InvoiceResponse>(StatusCodes.Status200OK, "text/event-stream")
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status500InternalServerError);
        return route;
    }

    private static RouteHandlerBuilder GetListInvoicesDocs(this RouteHandlerBuilder route)
    {
        route.WithName("ListInvoices")
             .WithSummary("Lista notas fiscais com paginação e busca")
             .WithDescription("Retorna a lista paginada de notas fiscais ordenadas por numeração sequencial.")
             .Produces<ListInvoicesResponse>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status400BadRequest)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status500InternalServerError);
        return route;
    }
    #endregion
}