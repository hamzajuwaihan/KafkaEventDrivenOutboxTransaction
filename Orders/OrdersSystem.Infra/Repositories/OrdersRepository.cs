using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrdersSystem.Domain.Entities;
using OrdersSystem.Domain.Enums;
using OrdersSystem.Infra.DB;

namespace OrdersSystem.Infra.Repositories;

public class OrdersRepository(AppDbContext appDbContext)
{
    private readonly AppDbContext _context = appDbContext;

    public async Task<Order?> GetOrder(Guid orderId) =>
    await _context.Orders
                       .Where(o => o.Id == orderId)
                       .FirstOrDefaultAsync();

    public async Task<Order> CreateOrder(Order order)
    {
        using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            await _context.Orders.AddAsync(order);

            OutboxMessage outboxMessage = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Topic = "OrderCreated",
                Message = JsonSerializer.Serialize(order),
                CreatedAt = DateTime.UtcNow,
                IsProcessed = false
            };
            await _context.OutboxMessages.AddAsync(outboxMessage);

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return order;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<Order> UpdateOrder(Guid id, string status)
    {
        Order? order = await _context.Orders.FindAsync(id);

        if (order == null)
        {
            throw new KeyNotFoundException($"Order with ID {id} not found.");
        }

        if (!Enum.TryParse(status, true, out OrderStatus newStatus))
        {
            throw new ArgumentException($"Invalid status value: {status}");
        }

        order.Status = newStatus;

        await _context.SaveChangesAsync();

        return order;
    }


}
