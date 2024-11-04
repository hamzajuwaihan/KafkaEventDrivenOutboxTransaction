using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrdersSystem.Domain.Entities;
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
        // Use a transaction to ensure atomicity
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            await _context.Orders.AddAsync(order);

            var outboxMessage = new OutboxMessage
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

}
