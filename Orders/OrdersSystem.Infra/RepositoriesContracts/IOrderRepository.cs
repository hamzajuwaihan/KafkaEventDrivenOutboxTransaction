using OrdersSystem.Domain.Entities;

namespace OrdersSystem.Infra.RepositoriesContracts;

public interface IOrderRepository
{
    Task<Order> CreateOrder(Order order);

    Task<Order> GetOrderById(Guid id);

    Task<Order> UpdateOrder(Guid id, string status);
}
