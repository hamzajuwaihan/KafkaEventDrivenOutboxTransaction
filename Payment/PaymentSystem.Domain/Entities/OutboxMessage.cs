namespace PaymentSystem.Domain.Entities;

public class OutboxMessage
{
    public Guid Id { get; set; }
    public required string  Topic { get; set; }
    public required string Message { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
    public bool IsProcessed { get; set; }
    public OutboxMessage() { }

}
