namespace Domain.Messaging;

public interface IDeadLetterEntity
{
    Guid Id { get; }
    string Type { get; }
    string Content { get; }
    string Error { get; }
    DateTime DeadLetteredAt { get; }
    DateTime? CompensatedAt { get; }
    string? CompensationError { get; }

    void MarkAsCompensated();
    void MarkCompensationFailed(string error);
}
