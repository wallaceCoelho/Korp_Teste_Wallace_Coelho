using Domain.Messaging;

namespace Infraestructure.Messaging.Interfaces;

public interface IRabbitMqProducer
{
    Task PublishAsync(ProducerOptions options, string messageType, string payload, CancellationToken cancellationToken);
    Task<bool> ShouldThrottleAsync(ProducerOptions options, CancellationToken cancellationToken);
}
