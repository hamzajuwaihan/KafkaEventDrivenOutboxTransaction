namespace PaymentSystem.Infra.Messages;

public class OrderCreatedMessage
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; }
}
