using Application.Common.Queries;
using Application.Features.Products.CreateProduct;
using Application.Features.Products.DeleteProduct;
using Application.Features.Products.GetProducts;
using Application.Features.Products.UpdateProduct;
using Application.Features.Products.UpdateStock;
using Mediator;
using Presentation.API.Extensions;

namespace Presentation.API.Endpoints;

public static class ProductsEndpoint
{
    public static void MapProductsEndpoints(this IEndpointRouteBuilder app)
    {
        const string route = "/api/products";
        var group = app.MapGroup(route).WithTags("Products");

        group.MapPost("/", async (CreateProductCommand command, IMediator mediator) =>
        {
            var result = await mediator.Send(command);
            return result.ToHttpResult((id) => Results.Created($"{route}/{id}", new { id }));
        }).GetCreateProductDocs();

        group.MapPut("/{id:guid}", async (Guid id, UpdateProductCommand command, IMediator mediator) =>
        {
            var result = await mediator.Send(command with { Id = id });
            return result.ToHttpResult();
        }).GetUpdateProductDocs();

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetProductQuery(id));
            return result.ToHttpResult();
        }).GetGetProductByIdDocs();

        group.MapGet("/", async ([AsParameters] QueryParams query, IMediator mediator) =>
        {
            var result = await mediator.Send(new ListProductsQuery(query));
            return result.ToHttpResult();
        }).GetListProductsDocs();

        group.MapPatch("/{id:guid}/stock", async (Guid id, UpdateStockCommand command, IMediator mediator) =>
        {
            var result = await mediator.Send(command with { ProductId = id });
            return result.ToHttpResult();
        }).GetUpdateProductStockDocs();

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new DeleteProductCommand(id));
            return result.ToHttpResult();
        }).GetDeleteProductDocs();
    }

    #region DOCUMENTAÇÃO
    private static RouteHandlerBuilder GetCreateProductDocs(this RouteHandlerBuilder route)
    {
        route.WithName("CreateProduct")
             .WithSummary("Cria um novo produto no inventário")
             .WithDescription("Cadastra um produto com código único, nome obrigatório, preço unitário, estoque inicial e descrição opcional.")
             .Produces<Guid>(StatusCodes.Status201Created)
             .ProducesProblem(StatusCodes.Status400BadRequest)
             .ProducesProblem(StatusCodes.Status409Conflict)
             .ProducesProblem(StatusCodes.Status500InternalServerError);
        return route;
    }

    private static RouteHandlerBuilder GetUpdateProductDocs(this RouteHandlerBuilder route)
    {
        route.WithName("UpdateProduct")
             .WithSummary("Atualiza um produto existente")
             .WithDescription("Altera o código, nome, preço unitário, estoque mínimo, categoria e descrição de um produto.")
             .Produces<Guid>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status400BadRequest)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status409Conflict)
             .ProducesProblem(StatusCodes.Status500InternalServerError);
        return route;
    }

    private static RouteHandlerBuilder GetGetProductByIdDocs(this RouteHandlerBuilder route)
    {
        route.WithName("GetProductById")
             .WithSummary("Obtém um produto pelo ID")
             .WithDescription("Recupera os detalhes completos de um produto específico no catálogo pelo seu GUID.")
             .Produces<ProductResponse>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status400BadRequest)
             .ProducesProblem(StatusCodes.Status500InternalServerError);
        return route;
    }

    private static RouteHandlerBuilder GetListProductsDocs(this RouteHandlerBuilder route)
    {
        route.WithName("ListProducts")
             .WithSummary("Lista produtos com paginação e busca")
             .WithDescription("Retorna uma lista paginada de produtos, permitindo busca por código, nome ou descrição.")
             .Produces<ListProductsResponse>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status400BadRequest)
             .ProducesProblem(StatusCodes.Status500InternalServerError);
        return route;
    }

    private static RouteHandlerBuilder GetUpdateProductStockDocs(this RouteHandlerBuilder route)
    {
        route.WithName("UpdateProductStock")
             .WithSummary("Atualiza a quantidade de estoque de um produto")
             .WithDescription("Adiciona ou deduz unidades do estoque de um produto utilizando controle de resiliência Polly contra concorrência.")
             .Produces<Guid>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status400BadRequest)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status409Conflict)
             .ProducesProblem(StatusCodes.Status500InternalServerError);
        return route;
    }

    private static RouteHandlerBuilder GetDeleteProductDocs(this RouteHandlerBuilder route)
    {
        route.WithName("DeleteProduct")
             .WithSummary("Exclui um produto (Soft Delete)")
             .WithDescription("Marca o produto como excluído no inventário.")
             .Produces<Guid>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status400BadRequest)
             .ProducesProblem(StatusCodes.Status500InternalServerError);
        return route;
    }
    #endregion
}
