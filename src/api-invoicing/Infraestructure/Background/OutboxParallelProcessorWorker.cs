#pragma warning disable CA1873

using Domain.Entities;
using Infraestructure.Messaging.Configuration;
using Infraestructure.Messaging.Interfaces;
using Infraestructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infraestructure.Background;

internal sealed class OutboxParallelProcessorWorker(
    IServiceScopeFactory scopeFactory,
    MessagingRegistry registry,
    IRabbitMqProducer producer,
    ILogger<OutboxParallelProcessorWorker> logger) : BackgroundService
{
    private const int BatchSize = 30;
    private const int MaxParallelism = 5;
    private const int MaxRetries = 3;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_interval);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Erro inesperado no loop do Outbox Worker.");
            }
        }
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        List<OutboxMessage> messages;
        using (var scope = scopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<InvoicingDbContext>();

            messages = await dbContext.OutboxMessages
                .AsNoTracking()
                .Where(m => m.RetryCount < MaxRetries)
                .OrderBy(m => m.CreatedAt)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);
        }

        if (messages.Count == 0) return;

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = MaxParallelism,
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(messages, parallelOptions, async (message, ct) =>
        {
            await PublishAsParallel(message, ct);
        });
    }

    private async Task PublishAsParallel(OutboxMessage message, CancellationToken cancellationToken)
    {
        using var taskScope = scopeFactory.CreateScope();
        var dbContext = taskScope.ServiceProvider.GetRequiredService<InvoicingDbContext>();

        if (!registry.Producers.TryGetValue(message.Type, out var options))
        {
            var error = $"Configuração de produtor não encontrada para '{message.Type}'";
            logger.LogCritical("Erro fatal no Outbox: {Error}", error);
            await HandlePersistentFailureAsync(dbContext, message, error);
            return;
        }

        if (await producer.ShouldThrottleAsync(options, cancellationToken))
        {
            return;
        }

        try
        {
            await producer.PublishAsync(options, message.Type, message.Content, cancellationToken);
            await dbContext.OutboxMessages
                .Where(m => m.Id == message.Id)
                .ExecuteDeleteAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            int currentAttempt = message.RetryCount + 1;

            if (currentAttempt >= MaxRetries)
            {
                logger.LogCritical(ex,
                    "[DEAD LETTER] Mensagem {Id} ({Type}) atingiu o limite de {Max} tentativas e foi movida para quarentena.",
                    message.Id, message.Type, MaxRetries);

                await HandlePersistentFailureAsync(dbContext, message, ex.Message);
            }
            else
            {
                logger.LogWarning(ex,
                    "Falha temporária ao publicar mensagem Outbox {Id}. Tentativa {Attempt}/{Max}",
                    message.Id, currentAttempt, MaxRetries);

                try
                {
                    await dbContext.OutboxMessages
                        .Where(m => m.Id == message.Id)
                        .ExecuteUpdateAsync(setters => setters
                            .SetProperty(m => m.RetryCount, currentAttempt), CancellationToken.None);
                }
                catch (Exception updateEx)
                {
                    logger.LogError(updateEx, "Erro ao atualizar RetryCount para a mensagem Outbox {Id}.", message.Id);
                }
            }
        }
    }

    private async Task HandlePersistentFailureAsync(
        InvoicingDbContext dbContext,
        OutboxMessage message,
        string errorMessage)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var safeToken = cts.Token;

        try
        {
            var strategy = dbContext.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await dbContext.Database.BeginTransactionAsync(safeToken);

                var deadLetter = OutboxDeadLetter.CreateFromOutbox(message, errorMessage);
                await dbContext.OutboxDeadLetters.AddAsync(deadLetter, safeToken);
                await dbContext.SaveChangesAsync(safeToken);

                await dbContext.OutboxMessages
                    .Where(m => m.Id == message.Id)
                    .ExecuteDeleteAsync(safeToken);

                await transaction.CommitAsync(safeToken);
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Falha crítica ao persistir Dead Letter para a mensagem Outbox {Id}. Erro original: {OriginalError}",
                message.Id, errorMessage);
        }
    }
}
