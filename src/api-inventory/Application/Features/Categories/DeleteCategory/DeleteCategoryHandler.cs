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
        var category = await unitWork.AsQueryable<Category>().FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
        if (category is null)
            return new ApiError(ErrorType.NotFound, "Categoria não encontrada");

        var canDeleteResult = category.CanDelete();
        if (!canDeleteResult.IsSuccess)
            return new ApiError(ErrorType.Conflict, canDeleteResult.Error!);

        unitWork.Delete(category);
        await unitWork.SaveChangesAsync(cancellationToken);

        return request.Id;
    }
}
