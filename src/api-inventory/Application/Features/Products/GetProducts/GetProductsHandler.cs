using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Domain.Entities;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Products.GetProducts;

public sealed class GetCategoryHandler(IUnitWork unitWork) : IRequestHandler<GetProductQuery, QueryResult<ProductResponse>>
{
    public async ValueTask<QueryResult<ProductResponse>> Handle(GetProductQuery request, CancellationToken cancellationToken)
    {
        var product = await unitWork.AsQueryable<Product>()
            .AsNoTracking()
            .Where(p => p.Id == request.Id)
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
            .FirstOrDefaultAsync(cancellationToken);

        return product is null
            ? new ApiError(ErrorType.NotFound, "Produto não encontrado.")
            : product;
    }
}
