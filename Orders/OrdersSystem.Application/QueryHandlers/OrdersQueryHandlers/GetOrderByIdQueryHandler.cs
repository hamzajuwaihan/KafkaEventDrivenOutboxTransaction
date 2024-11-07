using MediatR;
using OrdersSystem.Application.Queries.OrderQueries;
using OrdersSystem.Domain.Entities;
using OrdersSystem.Infra.RepositoriesContracts;

namespace OrdersSystem.Application.QueryHandlers.OrdersQueryHandlers;

public class GetOrderByIdByIdQueryHandler(IOrderRepository orderRepository) : IRequestHandler<GetOrderByIdByIdQuery, Order>
{
    private readonly IOrderRepository _orderRepository = orderRepository;
    public async Task<Order> Handle(GetOrderByIdByIdQuery request, CancellationToken cancellationToken) => await _orderRepository.GetOrderById(request.Id);
}
