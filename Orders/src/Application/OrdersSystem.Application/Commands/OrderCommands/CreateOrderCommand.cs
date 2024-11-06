using MediatR;
using OrdersSystem.Domain.Entities;

namespace OrdersSystem.Application.Commands.OrderCommands;

public class CreateOrderCommand(int amount) : IRequest<Order>
{
    public int Amount { get; } = amount;
}
