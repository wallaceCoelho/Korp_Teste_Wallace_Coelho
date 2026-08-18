#pragma warning disable CA1873

using Domain.Entities;
using Domain.Messaging;
using Infraestructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Infraestructure.Background;

internal sealed class InboxParallelProcessorWorker(IServiceScopeFactory scopeFactory, ILogger<InboxParallelProcessorWorker> logger) : BackgroundService
{
    private const int BatchSize = 30;
    private const int MaxParallelism = 5;
    private const int MaxRetries = 3;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Inbox Processor Worker ativo.");
        using var timer = new PeriodicTimer(_interval);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessInboxBatchAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Erro no loop do Inbox Processor Worker.");
            }
        }
    }

    private async Task ProcessInboxBatchAsync(CancellationToken cancellationToken)
    {
        List<InboxMessage> pendingMessages;
        using (var scope = scopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<InvoicingDbContext>();

            pendingMessages = await dbContext.InboxMessages
                .AsNoTracking()
                .Where(m => m.RetryCount < MaxRetries)
                .OrderBy(m => m.ReceivedAt)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);
        }

        if (pendingMessages.Count == 0) return;

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = MaxParallelism,
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(pendingMessages, parallelOptions, async (message, ct) =>
        {
            using var taskScope = scopeFactory.CreateScope();
            var dbContext = taskScope.ServiceProvider.GetRequiredService<InvoicingDbContext>();

            var eventType = EventTypeRegistry.GetType(message.Type);
            if (eventType is null)
            {
                await HandleDeadLetterWithCompensationAsync(taskScope, dbContext, message, null, null, $"Tipo não registrado: {message.Type}");
                return;
            }

            object? eventData;
            try
            {
                eventData = JsonSerializer.Deserialize(message.Content, eventType) ?? throw new JsonException("Payload nulo");
            }
            catch (Exception ex)
            {
                await HandleDeadLetterWithCompensationAsync(taskScope, dbContext, message, eventType, null, $"JSON Inválido: {ex.Message}");
                return;
            }

            try
            {
                var strategy = dbContext.Database.CreateExecutionStrategy();

                await strategy.ExecuteAsync(async () =>
                {
                    await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);

                    var handlerType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
                    dynamic handler = taskScope.ServiceProvider.GetRequiredService(handlerType);
                    dynamic dynamicEvent = eventData;

                    await handler.HandleAsync(dynamicEvent, ct);

                    await dbContext.InboxMessages
                        .Where(i => i.Id == message.Id)
                        .ExecuteDeleteAsync(ct);

                    await transaction.CommitAsync(ct);
                });
            }
            catch (Exception ex)
            {
                int currentAttempt = message.RetryCount + 1;

                if (currentAttempt >= MaxRetries)
                {
                    logger.LogCritical(ex, "[INBOX DEAD LETTER] Mensagem {Id} ({Type}) excedeu {Max} tentativas. Executando compensação.",
                        message.Id, message.Type, MaxRetries);

                    await HandleDeadLetterWithCompensationAsync(taskScope, dbContext, message, eventType, eventData, ex.Message);
                }
                else
                {
                    logger.LogWarning(ex, "Falha temporária no Inbox {Id}. Tentativa {Attempt}/{Max}",
                        message.Id, currentAttempt, MaxRetries);

                    try
                    {
                        await dbContext.InboxMessages
                            .Where(i => i.Id == message.Id)
                            .ExecuteUpdateAsync(setters => setters
                                .SetProperty(i => i.RetryCount, currentAttempt), CancellationToken.None);
                    }
                    catch (Exception updateEx)
                    {
                        logger.LogError(updateEx, "Erro ao atualizar RetryCount para a mensagem Inbox {Id}.", message.Id);
                    }
                }
            }
        });
    }

    private async Task HandleDeadLetterWithCompensationAsync(
        IServiceScope taskScope,
        InvoicingDbContext dbContext,
        InboxMessage message,
        Type? eventType,
        object? eventData,
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

                if (eventType is not null && eventData is not null)
                {
                    var compensationHandlerType = typeof(IDeadLetterHandler<>).MakeGenericType(eventType);
                    var compensationHandler = taskScope.ServiceProvider.GetService(compensationHandlerType);

                    if (compensationHandler is not null)
                    {
                        dynamic dynamicHandler = compensationHandler;
                        dynamic dynamicEvent = eventData;
                        await dynamicHandler.CompensateAsync(dynamicEvent, errorMessage, safeToken);
                    }
                }

                var deadLetter = InboxDeadLetter.CreateFromInbox(message, errorMessage);
                await dbContext.InboxDeadLetters.AddAsync(deadLetter, safeToken);
                await dbContext.SaveChangesAsync(safeToken);

                await dbContext.InboxMessages
                    .Where(i => i.Id == message.Id)
                    .ExecuteDeleteAsync(safeToken);

                await transaction.CommitAsync(safeToken);
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha crítica ao persistir Dead Letter para a mensagem Inbox {Id}. Erro original: {OriginalError}",
                message.Id, errorMessage);
        }
    }
}