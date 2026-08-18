using Domain.Messaging;

namespace Domain.Entities;

public sealed class InboxDeadLetter : IDeadLetterEntity
{
    public Guid Id { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string Error { get; init; } = string.Empty;
    public int Attempts { get; init; }
    public DateTime ReceivedAt { get; init; }
    public DateTime DeadLetteredAt { get; init; }
    public DateTime? CompensatedAt { get; private set; }
    public string? CompensationError { get; private set; }

    public static InboxDeadLetter CreateFromInbox(Domain.Entities.InboxMessage msg, string finalError) => new()
    {
        Id = msg.Id,
        Type = msg.Type,
        Content = msg.Content,
        Error = finalError,
        Attempts = msg.RetryCount + 1,
        ReceivedAt = msg.ReceivedAt,
        DeadLetteredAt = DateTime.UtcNow
    };

    public void MarkAsCompensated()
    {
        CompensatedAt = DateTime.UtcNow;
        CompensationError = null;
    }

    public void MarkCompensationFailed(string error) => CompensationError = error;
}
