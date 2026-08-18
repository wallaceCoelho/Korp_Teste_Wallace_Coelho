using Domain.Messaging;
using Infraestructure.Messaging.Interfaces;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using System.Collections.Concurrent;
using System.Text;

namespace Infraestructure.Messaging.RabbitMQ;

internal sealed class RabbitMqProducer : IRabbitMqProducer
{
    private readonly IConnection _connection;
    private readonly ILogger<RabbitMqProducer> _logger;
    private readonly ResiliencePipeline _resiliencePipeline;
    private readonly ConcurrentDictionary<string, bool> _throttleStates = new();

    public RabbitMqProducer(IConnection connection, ILogger<RabbitMqProducer> logger)
    {
        _connection = connection;
        _logger = logger;

        _resiliencePipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(500),
                BackoffType = DelayBackoffType.Exponential,
                OnRetry = args =>
                {
                    var exception = args.Outcome.Exception;
                    _logger.LogWarning(exception, "Falha ao comunicar com RabbitMQ. Tentativa {AttemptNumber}.", args.AttemptNumber);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    public async Task<bool> ShouldThrottleAsync(ProducerOptions options, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(options.MonitoredQueue))
            return false;

        try
        {
            using var channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
            var queueInfo = await channel.QueueDeclarePassiveAsync(options.MonitoredQueue, cancellationToken: cancellationToken);
            var count = queueInfo.MessageCount;

            var isCurrentlyThrottled = _throttleStates.GetValueOrDefault(options.MonitoredQueue, false);

            if (!isCurrentlyThrottled && count >= (options.MaxQueueCapacity * 0.80))
            {
                _throttleStates[options.MonitoredQueue] = true;
                _logger.LogWarning("Backpressure ATIVADO! Fila '{Queue}' atingiu {Count} msgs (>= 80%).",
                    options.MonitoredQueue, count);
                return true;
            }

            if (isCurrentlyThrottled && count <= (options.MaxQueueCapacity * 0.30))
            {
                _throttleStates[options.MonitoredQueue] = false;
                return false;
            }

            return isCurrentlyThrottled;
        }
        catch
        {
            return false;
        }
    }

    public async Task PublishAsync(ProducerOptions options, string messageType, string payload, CancellationToken cancellationToken)
    {
        await _resiliencePipeline.ExecuteAsync(async ct =>
        {
            using var channel = await _connection.CreateChannelAsync(cancellationToken: ct);

            await channel.ExchangeDeclareAsync(
                exchange: options.Exchange,
                type: options.ExchangeType,
                durable: true,
                cancellationToken: ct);

            var body = Encoding.UTF8.GetBytes(payload);
            var properties = new BasicProperties
            {
                DeliveryMode = DeliveryModes.Persistent,
                Type = messageType,
                MessageId = Guid.NewGuid().ToString(),
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            };

            await channel.BasicPublishAsync(
                exchange: options.Exchange,
                routingKey: options.RoutingKey,
                mandatory: true,
                basicProperties: properties,
                body: body,
                cancellationToken: ct);
        }, cancellationToken);
    }
}
