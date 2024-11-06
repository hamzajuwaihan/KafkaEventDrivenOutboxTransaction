using System.Text.Json;
using MediatR;
using OrdersSystem.Application.Commands.OrderCommands;
using OrdersSystem.Domain.Entities;
using OrdersSystem.Domain.Enums;
using OrdersSystem.Infra.Producers;
using OrdersSystem.Infra.RepositoriesContracts;

namespace OrdersSystem.Application.CommandHandlers.OrderCommandHandlers;

public class CreateOrderCommandHandler(IOrderRepository orderRepository) : IRequestHandler<CreateOrderCommand, Order>
{
    private readonly IOrderRepository _orderRepository = orderRepository;
    public async Task<Order> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        Guid orderId = Guid.NewGuid();
        Order order = new()
        {
            Id = orderId,
            Amount = request.Amount,
            Status = OrderStatus.Pending,
        };

        Order createdOrder = await _orderRepository.CreateOrder(order);

        return createdOrder;
    }
}
