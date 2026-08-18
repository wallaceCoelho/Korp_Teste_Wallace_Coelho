namespace Domain.Entities;

public sealed class InboxMessage
{
    public Guid Id { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public DateTime ReceivedAt { get; init; }
    public int RetryCount { get; private set; }

    public static InboxMessage Create(Guid messageId, string eventType, string content)
    {
        if (messageId == Guid.Empty)
            throw new ArgumentException("Message ID cannot be empty.", nameof(messageId));

        if (string.IsNullOrEmpty(eventType))
            throw new ArgumentException("Event type cannot be empty.", nameof(eventType));

        if (string.IsNullOrEmpty(content))
            throw new ArgumentException("Content cannot be empty.", nameof(content));
    
        return new InboxMessage
        {
            Id = messageId,
            Type = eventType,
            Content = content,
            ReceivedAt = DateTime.UtcNow
        };
    }
}
