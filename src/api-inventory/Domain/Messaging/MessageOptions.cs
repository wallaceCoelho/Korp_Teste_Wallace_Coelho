namespace Domain.Messaging;

public sealed class ProducerOptions
{
    public string Exchange { get; set; } = string.Empty;
    public string RoutingKey { get; set; } = string.Empty;
    public string ExchangeType { get; set; } = "topic";
    public string? MonitoredQueue { get; set; }
    public uint MaxQueueCapacity { get; set; } = 5000;
}

public sealed class ConsumerOptions
{
    public string QueueName { get; set; } = string.Empty;
    public string Exchange { get; set; } = string.Empty;
    public string ExchangeType { get; set; } = "topic";
    public List<string> RoutingKeys { get; set; } = [];
    public ushort PrefetchCount { get; set; } = 20;
}

public interface IProducerConfig<TEvent> where TEvent : IIntegrationEvent
{
    static abstract void Configure(ProducerOptions options);
}

public interface IConsumerConfig
{
    static abstract void Configure(ConsumerOptions options);
}
