using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Domain.Entities;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Products.UpdateProduct;

public sealed class UpdateProductHandler(IUnitWork unitWork) : IRequestHandler<UpdateProductCommand, CommandResult<Guid>>
{
    public async ValueTask<CommandResult<Guid>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await unitWork.AsQueryable<Product>().FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
        if (product is null)
            return new ApiError(ErrorType.NotFound, "Produto não encontrado.");

        if (request.Code != product.Code)
        {
            var exists = await unitWork.AsQueryable<Product>().AnyAsync(p => p.Code == request.Code && p.Id != request.Id, cancellationToken);
            if (exists)
                return new ApiError(ErrorType.Conflict, $"Já existe um produto com o código '{request.Code}'.");
        }

        var domainResult = product.UpdateDetails(
            request.Code,
            request.Name,
            request.UnitPrice,
            request.Description,
            request.MinStock);
        if (!domainResult.IsSuccess)
            return new ApiError(ErrorType.BadRequest, domainResult.Error!);

        if (request.CategoryId != product.CategoryId)
        {
            var categoryExists = await unitWork.AsQueryable<Category>().AnyAsync(c => c.Id == request.CategoryId, cancellationToken);
            if (!categoryExists)
                return new ApiError(ErrorType.BadRequest, "Categoria inválida.");
            product.ChangeCategory(request.CategoryId);
        }

        await unitWork.SaveChangesAsync(cancellationToken);
        return product.Id;
    }
}
