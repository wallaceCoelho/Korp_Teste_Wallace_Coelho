using Application.Common.Results;
using Mediator;

namespace Application.Features.Categories.UpdateCategory;

public sealed record UpdateCategoryCommand(
    Guid Id,
    string Name
) : IRequest<CommandResult<Guid>>;
