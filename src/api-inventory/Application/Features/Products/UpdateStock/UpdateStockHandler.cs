using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Domain.Entities;
using Domain.Enums;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace Application.Features.Products.UpdateStock;

public sealed class UpdateStockHandler(IUnitWork unitWork, ILogger<UpdateStockHandler> logger) : IRequestHandler<UpdateStockCommand, CommandResult<Guid>>
{
    private readonly ResiliencePipeline _retryPipeline = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            ShouldHandle = new PredicateBuilder().Handle<DbUpdateConcurrencyException>(),
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(50),
            BackoffType = DelayBackoffType.Exponential
        })
        .Build();

    public async ValueTask<CommandResult<Guid>> Handle(UpdateStockCommand request, CancellationToken cancellationToken)
    {
        try
        {
            return await _retryPipeline.ExecuteAsync(async ct =>
            {
                var product = await unitWork.AsQueryable<Product>()
                    .FirstOrDefaultAsync(p => p.Id == request.ProductId, ct);

                if (product is null)
                    return new ApiError(ErrorType.NotFound, "Produto não encontrado.");

                await unitWork.ReloadAsync(product, cancellationToken);

                var domainResult = request.Operation switch
                {
                    StockOperationType.Deduct => product.DeductStock(request.Quantity),
                    _ => product.AddStock(request.Quantity)
                };

                if (!domainResult.IsSuccess)
                    return new ApiError(ErrorType.BadRequest, domainResult.Error!);

                await unitWork.SaveChangesAsync(ct);
                return (CommandResult<Guid>)product.Id;

            }, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            logger.LogError("Concorrência detectada ao tentar atualizar o estoque do produto {ProductId}.", request.ProductId);
            return new ApiError(
                ErrorType.Conflict,
                "Não foi possível atualizar o estoque após múltiplas tentativas."
            );
        }
    }
}
