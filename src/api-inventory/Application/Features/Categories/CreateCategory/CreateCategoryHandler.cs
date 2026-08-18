using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Domain.Entities;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Categories.CreateCategory;

public sealed class CreateCategoryHandler(IUnitWork unitWork) : IRequestHandler<CreateCategoryCommand, CommandResult<Guid>>
{
    public async ValueTask<CommandResult<Guid>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var domainResult = Category.Create(request.Name);

        if (!domainResult.IsSuccess || domainResult.Value == null)
            return new ApiError(ErrorType.BadRequest, domainResult.Error!);

        var exists = await unitWork.AsQueryable<Category>().AnyAsync(c => c.Name == request.Name, cancellationToken);
        if (exists)
            return new ApiError(ErrorType.Conflict, $"Já existe uma categoria com o nome '{request.Name}'.");

        var category = domainResult.Value;

        await unitWork.AddAsync(category, cancellationToken);
        await unitWork.SaveChangesAsync(cancellationToken);

        return category.Id;
    }
}
