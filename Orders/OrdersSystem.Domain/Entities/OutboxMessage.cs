using System;

namespace OrdersSystem.Domain.Entities;

public class OutboxMessage
{
    public Guid Id { get; set; }
    public string Topic { get; set; }
    public string Message { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsProcessed { get; set; }

    public OutboxMessage() { }
}
