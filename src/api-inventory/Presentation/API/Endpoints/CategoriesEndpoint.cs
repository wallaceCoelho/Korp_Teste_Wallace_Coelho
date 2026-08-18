using Application.Common.Queries;
using Application.Features.Categories.CreateCategory;
using Application.Features.Categories.DeleteCategory;
using Application.Features.Categories.GetCategory;
using Application.Features.Categories.UpdateCategory;
using Mediator;
using Presentation.API.Extensions;

namespace Presentation.API.Endpoints;

public static class CategoriesEndpoint
{
    public static void MapCategoriesEndpoints(this IEndpointRouteBuilder app)
    {
        const string route = "/api/categories";
        var group = app.MapGroup(route).WithTags("Categories");

        group.MapPost("/", async (CreateCategoryCommand command, IMediator mediator) =>
        {
            var result = await mediator.Send(command);
            return result.ToHttpResult((id) => Results.Created($"{route}/{id}", new { id }));
        }).GetCreateCategoryDocs();

        group.MapPut("/{id:guid}", async (Guid id, UpdateCategoryCommand command, IMediator mediator) =>
        {
            var result = await mediator.Send(command with { Id = id });
            return result.ToHttpResult();
        }).GetUpdateCategoryDocs();

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetCategoryQuery(id));
            return result.ToHttpResult();
        }).GetGetCategoryByIdDocs();

        group.MapGet("/", async ([AsParameters] QueryParams query, IMediator mediator) =>
        {
            var result = await mediator.Send(new ListCategoriesQuery(query));
            return result.ToHttpResult();
        }).GetListCategoriesDocs();

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new DeleteCategoryCommand(id));
            return result.ToHttpResult();
        }).GetDeleteCategoryDocs();
    }

    #region DOCUMENTAÇÃO

    private static RouteHandlerBuilder GetCreateCategoryDocs(this RouteHandlerBuilder route)
    {
        route.WithName("CreateCategory")
             .WithSummary("Cria uma nova categoria de produtos")
             .WithDescription("Cadastra uma nova categoria no inventário com o nome fornecido.")
             .Produces<Guid>(StatusCodes.Status201Created)
             .ProducesProblem(StatusCodes.Status400BadRequest)
             .ProducesProblem(StatusCodes.Status409Conflict)
             .ProducesProblem(StatusCodes.Status500InternalServerError);
        return route;
    }

    private static RouteHandlerBuilder GetUpdateCategoryDocs(this RouteHandlerBuilder route)
    {
        route.WithName("UpdateCategory")
             .WithSummary("Atualiza uma categoria existente")
             .WithDescription("Altera o nome da categoria identificada pelo GUID informado.")
             .Produces<Guid>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status400BadRequest)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status409Conflict)
             .ProducesProblem(StatusCodes.Status500InternalServerError);
        return route;
    }

    private static RouteHandlerBuilder GetGetCategoryByIdDocs(this RouteHandlerBuilder route)
    {
        route.WithName("GetCategoryById")
             .WithSummary("Obtém uma categoria pelo ID")
             .WithDescription("Retorna os detalhes completos de uma categoria através do seu identificador único.")
             .Produces<CategoryResponse>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status500InternalServerError);
        return route;
    }

    private static RouteHandlerBuilder GetListCategoriesDocs(this RouteHandlerBuilder route)
    {
        route.WithName("ListCategories")
             .WithSummary("Lista categorias com suporte a paginação e filtragem")
             .WithDescription("Retorna uma lista paginada de categorias cadastradas, permitindo filtro por nome.")
             .Produces<ListCategoriesResponse>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status500InternalServerError);
        return route;
    }

    private static RouteHandlerBuilder GetDeleteCategoryDocs(this RouteHandlerBuilder route)
    {
        route.WithName("DeleteCategory")
             .WithSummary("Exclui uma categoria")
             .WithDescription("Remove uma categoria caso não existam produtos vinculados a ela.")
             .Produces<Guid>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status400BadRequest)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status500InternalServerError);
        return route;
    }

    #endregion
}
