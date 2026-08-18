#pragma warning disable CA1873

using Domain.Entities;
using Infraestructure.Messaging.Configuration;
using Infraestructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Infraestructure.Background;

internal sealed class RabbitMqIngestionWorker(
    IConnection connection,
    IServiceScopeFactory scopeFactory,
    MessagingRegistry registry,
    ILogger<RabbitMqIngestionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        foreach (var consumerConfig in registry.Consumers)
        {
            var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

            var dlx = $"{consumerConfig.Exchange}.dlx";
            var dlq = $"{consumerConfig.QueueName}.dlq";
            var dlqKey = $"{consumerConfig.QueueName}.dlq-key";

            await channel.ExchangeDeclareAsync(consumerConfig.Exchange, consumerConfig.ExchangeType, durable: true, cancellationToken: stoppingToken);
            await channel.ExchangeDeclareAsync(dlx, ExchangeType.Direct, durable: true, cancellationToken: stoppingToken);
            await channel.QueueDeclareAsync(dlq, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
            await channel.QueueBindAsync(dlq, dlx, dlqKey, cancellationToken: stoppingToken);

            var queueArgs = new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = dlx,
                ["x-dead-letter-routing-key"] = dlqKey
            };

            await channel.QueueDeclareAsync(consumerConfig.QueueName, durable: true, exclusive: false, autoDelete: false, arguments: queueArgs, cancellationToken: stoppingToken);

            foreach (var routingKey in consumerConfig.RoutingKeys)
            {
                await channel.QueueBindAsync(consumerConfig.QueueName, consumerConfig.Exchange, routingKey, cancellationToken: stoppingToken);
            }

            await channel.BasicQosAsync(0, consumerConfig.PrefetchCount, false, stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                try
                {
                    var eventType = ea.BasicProperties.Type ?? string.Empty;
                    var messageId = Guid.TryParse(ea.BasicProperties.MessageId, out var id) ? id : Guid.NewGuid();
                    var content = Encoding.UTF8.GetString(ea.Body.ToArray());

                    using var scope = scopeFactory.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<InvoicingDbContext>();

                    bool exists = await dbContext.InboxMessages.AnyAsync(i => i.Id == messageId, stoppingToken);
                    if (!exists)
                    {
                        await dbContext.InboxMessages.AddAsync(InboxMessage.Create(messageId, eventType, content), stoppingToken);
                        await dbContext.SaveChangesAsync(stoppingToken);
                    }

                    await channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Falha ao persistir mensagem no Inbox. Enviando para DLQ.");
                    await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false);
                }
            };

            await channel.BasicConsumeAsync(consumerConfig.QueueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
            logger.LogInformation("Ingestor conectado e ouvindo a fila '{Queue}'.", consumerConfig.QueueName);
        }

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
