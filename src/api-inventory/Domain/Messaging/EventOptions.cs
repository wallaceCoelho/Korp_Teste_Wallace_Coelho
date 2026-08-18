namespace Domain.Messaging;

public interface IIntegrationEvent
{
    Guid Id { get; }
    DateTime OccurredOn { get; }
}

public interface IIntegrationEventHandler<in TEvent> where TEvent : IIntegrationEvent
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken);
}

public interface IDeadLetterHandler<in TEvent> where TEvent : IIntegrationEvent
{
    Task CompensateAsync(TEvent @event, string failureReason, CancellationToken cancellationToken);
}

public static class EventTypeRegistry
{
    private static readonly Dictionary<string, Type> _types = [];

    public static void Register(Type eventType)
    {
        _types[eventType.Name] = eventType;
    }

    public static Type? GetType(string eventTypeName) => _types.GetValueOrDefault(eventTypeName);
}