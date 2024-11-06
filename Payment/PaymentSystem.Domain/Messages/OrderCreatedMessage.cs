namespace PaymentSystem.Domain.Messages;

public class OrderCreatedMessage
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public required string Status { get; set; }
}
