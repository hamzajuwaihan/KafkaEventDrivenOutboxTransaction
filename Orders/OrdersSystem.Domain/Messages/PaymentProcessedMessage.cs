namespace OrdersSystem.Domain.Messages;

public class PaymentProcessedMessage
{
    public Guid Id { get; set; }
    public required string Status { get; set;}
    public required string OrderId { get; set; }
    public int Amount { get; set; }
}
