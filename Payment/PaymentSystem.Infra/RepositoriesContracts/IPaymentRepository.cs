using PaymentSystem.Domain.Entities;

namespace PaymentSystem.Infra.RepositoriesContracts;

public interface IPaymentRepository
{
    public Task<Payment> GetPaymentById(Guid id);
    public Task<Payment> CreatePayment(Guid orderId, decimal amount);
}
