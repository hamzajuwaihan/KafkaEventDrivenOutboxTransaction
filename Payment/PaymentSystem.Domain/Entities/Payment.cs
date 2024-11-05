
using System.Text.Json.Serialization;
using PaymentSystem.Domain.Enums;

namespace PaymentSystem.Domain.Entities;

public class Payment
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PaymentStatus Status { get; set; }
    public Payment() { }

}
