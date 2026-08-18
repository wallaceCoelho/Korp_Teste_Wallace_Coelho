using Application.Common.Queries;
using Application.Common.Results;
using Mediator;

namespace Application.Features.Categories.GetCategory;

public sealed record GetCategoryQuery(Guid Id) : IRequest<QueryResult<CategoryResponse>>;
public sealed record ListCategoriesQuery(QueryParams Query) : IRequest<QueryResult<ListCategoriesResponse>>;

public sealed record CategoryResponse(
    Guid Id,
    string Name,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public sealed record ProductCategoryResponse(
    Guid Id,
    string Name
);

public sealed record ListCategoriesResponse(List<CategoryResponse> Items, long TotalCount);
