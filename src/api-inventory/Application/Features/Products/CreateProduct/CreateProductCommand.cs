using Application.Common.Results;
using Mediator;

namespace Application.Features.Products.CreateProduct;

public sealed record CreateProductCommand(
    string Code,
    string Name,
    int InitialStock,
    decimal UnitPrice,
    string? Description = null,
    int? MinStock = null,
    Guid? CategoryId = null
) : IRequest<CommandResult<Guid>>;
