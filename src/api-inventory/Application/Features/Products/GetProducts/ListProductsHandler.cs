using Application.Common.Interfaces;
using Application.Common.Results;
using Domain.Entities;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Products.GetProducts;

public sealed class ListProductsHandler(IUnitWork unitWork) : IRequestHandler<ListProductsQuery, QueryResult<ListProductsResponse>>
{
    public async ValueTask<QueryResult<ListProductsResponse>> Handle(ListProductsQuery request, CancellationToken cancellationToken)
    {
        var query = unitWork.AsQueryable<Product>()
            .Include(p => p.Category)
            .AsNoTracking();

        var rawSearch = request.Query.Search?.Trim();
        if (!string.IsNullOrWhiteSpace(rawSearch))
        {
            var searchPattern = request.Query.GetSearchPattern();
            query = query.Where(p =>
                p.SearchVector.Matches(EF.Functions.PlainToTsQuery("portuguese", rawSearch)) ||
                EF.Functions.ILike(p.Name, searchPattern!) ||
                EF.Functions.ILike(p.Code, searchPattern!) ||
                (p.Description != null && EF.Functions.ILike(p.Description, searchPattern!))
            );
        }

        if (request.Query.CategoryId.HasValue && request.Query.CategoryId.Value != Guid.Empty)
        {
            query = query.Where(p => p.CategoryId == request.Query.CategoryId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        if (request.Query.IsPagination())
        {
            query = query.Skip(request.Query.GetSkip())
                         .Take(request.Query.GetPageSize() ?? 100);
        }

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new ProductResponse(
                p.Id,
                p.Code,
                p.Name,
                p.Description,
                p.StockQuantity,
                p.MinStockQuantity,
                p.UnitPrice,
                p.CreatedAt,
                p.UpdatedAt,
                p.CategoryId,
                p.Category != null ? new ProductCategoryResponse(p.Category.Id, p.Category.Name) : null
            ))
            .ToListAsync(cancellationToken);

        return new ListProductsResponse(items, totalCount);
    }
}
