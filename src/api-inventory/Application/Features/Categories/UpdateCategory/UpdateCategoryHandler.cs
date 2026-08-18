using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Domain.Entities;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Categories.UpdateCategory;

public sealed class UpdateCategoryHandler(IUnitWork unitWork) : IRequestHandler<UpdateCategoryCommand, CommandResult<Guid>>
{
    public async ValueTask<CommandResult<Guid>> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await unitWork.AsQueryable<Category>().FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
        if (category is null)
            return new ApiError(ErrorType.NotFound, "Categoria não encontrada.");

        if (request.Name != category.Name)
        {
            var exists = await unitWork.AsQueryable<Category>().AnyAsync(c => c.Name == request.Name && c.Id != request.Id, cancellationToken);
            if (exists)
                return new ApiError(ErrorType.Conflict, $"Já existe uma categoria com o nome '{request.Name}'.");
        }

        category.UpdateName(request.Name);
        await unitWork.SaveChangesAsync(cancellationToken);

        return category.Id;
    }
}
