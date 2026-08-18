using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Domain.Entities;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Categories.DeleteCategory;

public sealed class DeleteCategoryHandler(IUnitWork unitWork) : IRequestHandler<DeleteCategoryCommand, CommandResult<Guid>>
{
    public async ValueTask<CommandResult<Guid>> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await unitWork.AsQueryable<Category>()
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (category is null)
            return new ApiError(ErrorType.NotFound, "Categoria não encontrada");

        var linkedProducts = await unitWork.AsQueryable<Product>()
            .Where(p => p.CategoryId == request.Id)
            .ToListAsync(cancellationToken);

        foreach (var product in linkedProducts)
        {
            product.RemoveCategory();
        }

        unitWork.Delete(category);
        await unitWork.SaveChangesAsync(cancellationToken);

        return request.Id;
    }
}
