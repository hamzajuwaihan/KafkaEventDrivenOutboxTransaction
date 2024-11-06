
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using OrdersSystem.Application.Commands.OrderCommands;
using OrdersSystem.Application.Queries.OrderQueries;
using OrdersSystem.Domain.Entities;

namespace OrdersSystem.Presentation.Api.Routes;

public static class MapOrdersEndpoint
{
    public static void MapOrderEndpoint(this IEndpointRouteBuilder app)
    {

        RouteGroupBuilder group = app.MapGroup("/order");

        group.MapGet("{id:guid}", async (Guid id, IMediator _mediator) =>
        {
            Order result = await _mediator.Send(new GetOrderByIdByIdQuery(id));

            return Results.Ok(result);
        });

        group.MapPost("/Order", async (int amount, IMediator _mediator) =>
        {
            Order result = await _mediator.Send(new CreateOrderCommand(amount));

            return Results.Created($"/Order/{result.Id}", result);
        });

    }
}
