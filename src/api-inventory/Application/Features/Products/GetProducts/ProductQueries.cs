using Application.Common.Queries;
using Application.Common.Results;
using Mediator;

namespace Application.Features.Products.GetProducts;

public sealed record GetProductQuery(Guid Id) : IRequest<QueryResult<ProductResponse>>;
public sealed record ListProductsQuery(QueryParams Query) : IRequest<QueryResult<ListProductsResponse>>;

public sealed record ProductResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    int StockQuantity,
    int? MinStockQuantity,
    decimal UnitPrice,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    Guid? CategoryId,
    ProductCategoryResponse? Category
);

public sealed record ProductCategoryResponse(
    Guid Id,
    string Name
);

public sealed record ListProductsResponse(List<ProductResponse> Items, long TotalCount);
