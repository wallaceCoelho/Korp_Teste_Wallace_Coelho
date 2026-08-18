using Domain.Messaging;
using System.Text.Json;

namespace Domain.Entities;

public sealed class OutboxMessage
{
    public Guid Id { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public int RetryCount { get; private set; }

    public static OutboxMessage Create<TEvent>(TEvent @event) where TEvent : IIntegrationEvent
    {
        if (@event == null)
            throw new ArgumentNullException(nameof(@event));

        if (string.IsNullOrEmpty(@event.GetType().Name))
            throw new ArgumentException("Event type cannot be empty.");

        if (string.IsNullOrEmpty(JsonSerializer.Serialize(@event)))
            throw new ArgumentException("Event content cannot be empty.", nameof(@event));

        return new OutboxMessage
        {
            Id = @event.Id,
            Type = typeof(TEvent).Name,
            Content = JsonSerializer.Serialize(@event),
            CreatedAt = @event.OccurredOn
        };
    }
}
