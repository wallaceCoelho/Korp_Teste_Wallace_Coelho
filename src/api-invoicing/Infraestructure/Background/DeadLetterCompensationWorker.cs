#pragma warning disable CA1873

using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Messaging;
using Infraestructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Infraestructure.Background;

internal sealed class DeadLetterCompensationWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<DeadLetterCompensationWorker> logger) : BackgroundService
{
    private const int BatchSize = 20;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Dead Letter Compensation Worker unificado ativo.");
        using var timer = new PeriodicTimer(_interval);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessDeadLetterBatchAsync<OutboxDeadLetter>(stoppingToken);

                await ProcessDeadLetterBatchAsync<InboxDeadLetter>(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Erro no loop do Dead Letter Compensation Worker.");
            }
        }
    }

    private async Task ProcessDeadLetterBatchAsync<TDeadLetter>(CancellationToken cancellationToken)
        where TDeadLetter : class, IDeadLetterEntity
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InvoicingDbContext>();

        var deadLetters = await dbContext.Set<TDeadLetter>()
            .Where(d => d.CompensatedAt == null)
            .OrderBy(d => d.DeadLetteredAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (deadLetters.Count == 0) return;

        foreach (var deadLetter in deadLetters)
        {
            using var taskScope = scopeFactory.CreateScope();
            var unitWork = taskScope.ServiceProvider.GetRequiredService<IUnitWork>();

            try
            {
                var eventType = EventTypeRegistry.GetType(deadLetter.Type);
                if (eventType is null)
                {
                    deadLetter.MarkCompensationFailed($"Tipo de evento não registrado: '{deadLetter.Type}'");
                    await dbContext.SaveChangesAsync(cancellationToken);
                    continue;
                }

                var eventData = JsonSerializer.Deserialize(deadLetter.Content, eventType);
                if (eventData is null)
                {
                    deadLetter.MarkCompensationFailed("Conteúdo JSON inválido na Dead Letter.");
                    await dbContext.SaveChangesAsync(cancellationToken);
                    continue;
                }

                var handlerType = typeof(IDeadLetterHandler<>).MakeGenericType(eventType);
                var handler = taskScope.ServiceProvider.GetService(handlerType);

                if (handler is not null)
                {
                    await unitWork.ExecuteInTransactionAsync(async ct =>
                    {
                        dynamic dynamicHandler = handler;
                        dynamic dynamicEvent = eventData;

                        await dynamicHandler.CompensateAsync(dynamicEvent, deadLetter.Error, ct);

                        var dlEntry = await unitWork.AsQueryable<TDeadLetter>()
                            .FirstAsync(d => d.Id == deadLetter.Id, ct);

                        dlEntry.MarkAsCompensated();
                        unitWork.Update(dlEntry);

                    }, cancellationToken: cancellationToken);

                    logger.LogInformation("Compensação para [{Source}] {Id} ({Type}) concluída com sucesso.",
                        typeof(TDeadLetter).Name, deadLetter.Id, deadLetter.Type);
                }
                else
                {
                    deadLetter.MarkAsCompensated();
                    await dbContext.SaveChangesAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha ao compensar [{Source}] {Id} para o evento {Type}.",
                    typeof(TDeadLetter).Name, deadLetter.Id, deadLetter.Type);

                deadLetter.MarkCompensationFailed(ex.Message);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
    }
}