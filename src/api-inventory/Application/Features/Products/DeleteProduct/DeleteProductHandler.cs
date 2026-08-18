using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Domain.Entities;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Products.DeleteProduct;

public sealed class DeleteProductHandler(IUnitWork unitWork) : IRequestHandler<DeleteProductCommand, CommandResult<Guid>>
{
    public async ValueTask<CommandResult<Guid>> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await unitWork.AsQueryable<Product>().FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
        if (product is null)
            return new ApiError(ErrorType.NotFound, "Produto não encontrado");

        product.Delete();
        await unitWork.SaveChangesAsync(cancellationToken);

        return request.Id;
    }
}
