namespace Stingray.Domain.Outbox;

public class OutboxMessage
{
    public Guid Id { get; init; }
    public EventType EventType { get; init; }
    public string Payload { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? ProcessedAt { get; set; }
    public OutboxMessageStatus Status { get; set; }
}

public enum OutboxMessageStatus
{
    Processing,
    Processed,
    Failed
}

public enum EventType
{
    UserCreated,
    OrderCreated
}