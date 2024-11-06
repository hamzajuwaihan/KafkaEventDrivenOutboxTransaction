using MediatR;
using PaymentSystem.Domain.Entities;

namespace PaymentSystem.Application.Queries.PaymentQueries;

public class GetPaymentByIdQuery(Guid id) : IRequest<Payment>
{
    public Guid Id { get; } = id;
}
