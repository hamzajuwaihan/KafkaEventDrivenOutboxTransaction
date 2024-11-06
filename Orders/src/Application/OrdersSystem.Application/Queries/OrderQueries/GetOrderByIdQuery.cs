using MediatR;
using OrdersSystem.Domain.Entities;

namespace OrdersSystem.Application.Queries.OrderQueries;

public class GetOrderByIdByIdQuery(Guid id) : IRequest<Order>
{
    public Guid Id { get; } = id;
}
