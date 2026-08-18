using Domain.Messaging;

namespace Domain.Entities;

public sealed class OutboxDeadLetter : IDeadLetterEntity
{
    public Guid Id { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public string Error { get; private set; } = string.Empty;
    public int Attempts { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime DeadLetteredAt { get; private set; }
    public DateTime? CompensatedAt { get; private set; }
    public string? CompensationError { get; private set; }

    public static OutboxDeadLetter CreateFromOutbox(OutboxMessage msg, string finalError)
    {
        return new OutboxDeadLetter
        {
            Id = msg.Id,
            Type = msg.Type,
            Content = msg.Content,
            Error = finalError,
            Attempts = msg.RetryCount + 1,
            CreatedAt = msg.CreatedAt,
            DeadLetteredAt = DateTime.UtcNow
        };
    }

    public void MarkAsCompensated()
    {
        CompensatedAt = DateTime.UtcNow;
        CompensationError = null;
    }

    public void MarkCompensationFailed(string error) => CompensationError = error;
}
