using Application.Common.Results;
using Mediator;

namespace Application.Features.Categories.CreateCategory;

public sealed record CreateCategoryCommand(string Name) : IRequest<CommandResult<Guid>>;
