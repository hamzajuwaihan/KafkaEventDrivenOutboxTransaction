using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PaymentSystem.Domain.Entities;
using PaymentSystem.Domain.Enums;
using PaymentSystem.Infra.DB;

namespace PaymentSystem.Infra.Repositories;

public class PaymentRepository
{
    private readonly AppDbContext _context;
    private static readonly Random _random = new Random();

    public PaymentRepository(AppDbContext appDbContext)
    {
        _context = appDbContext;
    }

    public async Task<Payment> CreatePayment(Guid orderId, decimal amount)
    {
        Payment payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Amount = amount,
            Status = PaymentStatus.Pending
        };

        using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            bool isSuccessful = _random.NextDouble() >= 0.5;
            payment.Status = isSuccessful ? PaymentStatus.Successful : PaymentStatus.Failed;

            await _context.Payments.AddAsync(payment);

            OutboxMessage outboxMessage = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Topic = "PaymentProcessed",
                Message = JsonSerializer.Serialize(payment),
                CreatedAt = DateTime.UtcNow,
                IsProcessed = false
            };
            await _context.OutboxMessages.AddAsync(outboxMessage);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return payment;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    public async Task<Payment?> GetPayment(Guid paymentId)
    {
        return await _context.Payments
            .FirstOrDefaultAsync(p => p.Id == paymentId);
    }
}
