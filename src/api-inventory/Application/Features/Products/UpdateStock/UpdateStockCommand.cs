using Application.Common.Results;
using Domain.Enums;
using Mediator;

namespace Application.Features.Products.UpdateStock;

public sealed record UpdateStockCommand(
    Guid ProductId,
    int Quantity,
    StockOperationType Operation = StockOperationType.Add
) : IRequest<CommandResult<Guid>>;
