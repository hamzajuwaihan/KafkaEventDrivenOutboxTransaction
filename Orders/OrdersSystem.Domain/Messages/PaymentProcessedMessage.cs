namespace OrdersSystem.Domain.Messages;

public class PaymentProcessedMessage
{
    public Guid Id { get; set; }
    public string Status { get; set;}
    public string OrderId { get; set; }
    public int Amount { get; set; }
}
