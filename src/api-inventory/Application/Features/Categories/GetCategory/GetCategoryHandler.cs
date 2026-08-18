using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Domain.Entities;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Categories.GetCategory;

public sealed class GetCategoryHandler(IUnitWork unitWork) : IRequestHandler<GetCategoryQuery, QueryResult<CategoryResponse>>
{
    public async ValueTask<QueryResult<CategoryResponse>> Handle(GetCategoryQuery request, CancellationToken cancellationToken)
    {
        var category = await unitWork.AsQueryable<Category>()
            .AsNoTracking()
            .Where(c => c.Id == request.Id)
            .Select(c => new CategoryResponse(
                c.Id,
                c.Name,
                c.CreatedAt,
                c.UpdatedAt
            ))
            .FirstOrDefaultAsync(cancellationToken);

        if (category is null)
            return new ApiError(ErrorType.NotFound, $"Categoria não encontrada.");

        return category;
    }
}
