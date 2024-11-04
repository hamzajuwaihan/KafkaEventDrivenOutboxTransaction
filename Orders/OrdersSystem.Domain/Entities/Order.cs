using System.Text.Json.Serialization;
using OrdersSystem.Domain.Enums;

namespace OrdersSystem.Domain.Entities;

public class Order
{
    public Guid Id { get; init; }
    public int Amount { get; init; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public OrderStatus Status { get; set; }
    public Order() { }
}
