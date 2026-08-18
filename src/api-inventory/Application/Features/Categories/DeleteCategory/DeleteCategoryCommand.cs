using Application.Common.Results;
using Mediator;

namespace Application.Features.Categories.DeleteCategory;

public sealed record DeleteCategoryCommand(Guid Id) : IRequest<CommandResult<Guid>>;
