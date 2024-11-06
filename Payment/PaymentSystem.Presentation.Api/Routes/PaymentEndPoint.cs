using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PaymentSystem.Application.Queries.PaymentQueries;
using PaymentSystem.Domain.Entities;

namespace PaymentSystem.Presentation.Api.Routes;

public static class PaymentEndPoint
{
    public static void MapPaymentEndpoint(this IEndpointRouteBuilder app)
    {

        RouteGroupBuilder group = app.MapGroup("/payment");

        group.MapGet("{id:guid}", async (Guid id, IMediator _mediator) =>
        {
            Payment result = await _mediator.Send(new GetPaymentByIdQuery(id));

            return Results.Ok(result);
        });
    }
}
