using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Domain.Entities;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Products.CreateProduct;

public sealed class CreateProductHandler(IUnitWork unitWork) : IRequestHandler<CreateProductCommand, CommandResult<Guid>>
{
    public async ValueTask<CommandResult<Guid>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var domainResult = Product.Create(
            request.Code,
            request.Name,
            request.InitialStock,
            request.UnitPrice,
            request.Description,
            request.MinStock);

        if (!domainResult.IsSuccess || domainResult.Value == null)
            return new ApiError(ErrorType.BadRequest, domainResult.Error!);

        var exists = await unitWork.AsQueryable<Product>().AnyAsync(p => p.Code == request.Code, cancellationToken);
        if (exists)
            return new ApiError(ErrorType.Conflict, $"Já existe um produto com o código '{request.Code}'.");

        var product = domainResult.Value;

        if (request.CategoryId != product.CategoryId)
        {
            var categoryExists = await unitWork.AsQueryable<Category>().AnyAsync(c => c.Id == request.CategoryId, cancellationToken);
            if (!categoryExists)
                return new ApiError(ErrorType.BadRequest, "Categoria inválida.");

            product.ChangeCategory(request.CategoryId);
        }

        await unitWork.AddAsync(product, cancellationToken);
        await unitWork.SaveChangesAsync(cancellationToken);

        return product.Id;
    }
}