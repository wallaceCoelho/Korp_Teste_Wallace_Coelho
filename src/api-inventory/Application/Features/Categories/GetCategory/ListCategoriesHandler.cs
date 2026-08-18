using Application.Common.Interfaces;
using Application.Common.Results;
using Domain.Entities;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Categories.GetCategory;

public sealed class ListCategoriesHandler(IUnitWork unitWork) : IRequestHandler<ListCategoriesQuery, QueryResult<ListCategoriesResponse>>
{
    public async ValueTask<QueryResult<ListCategoriesResponse>> Handle(ListCategoriesQuery request, CancellationToken cancellationToken)
    {
        var query = unitWork.AsQueryable<Category>().AsNoTracking();

        string? searchPattern = request.Query.GetSearchPattern();
        if (!string.IsNullOrEmpty(searchPattern))
        {
            query = query.Where(p => EF.Functions.ILike(p.Name, searchPattern));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        if (request.Query.IsPagination())
        {
            query = query.Skip(request.Query.GetSkip())
                         .Take(request.Query.GetPageSize() ?? 100);
        }

        var items = await query
            .OrderBy(p => p.Name)
            .Select(p => new CategoryResponse(
                p.Id,
                p.Name,
                p.CreatedAt,
                p.UpdatedAt
            ))
            .ToListAsync(cancellationToken);

        return new ListCategoriesResponse(items, totalCount);
    }
}
