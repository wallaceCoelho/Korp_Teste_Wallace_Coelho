using Application.Common.Results;
using Mediator;

namespace Application.Features.Products.UpdateProduct;

public sealed record UpdateProductCommand(
    Guid Id,
    string Code,
    string Name,
    decimal UnitPrice,
    string? Description = null,
    int? MinStock = null,
    Guid? CategoryId = null
) : IRequest<CommandResult<Guid>>;
