using Application.Common.Results;
using Mediator;

namespace Application.Features.Products.DeleteProduct;

public sealed record DeleteProductCommand(Guid Id) : IRequest<CommandResult<Guid>>;
