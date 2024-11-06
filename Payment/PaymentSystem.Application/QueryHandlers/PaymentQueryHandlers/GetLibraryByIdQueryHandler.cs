using MediatR;
using PaymentSystem.Application.Queries.PaymentQueries;
using PaymentSystem.Domain.Entities;
using PaymentSystem.Infra.RepositoriesContracts;

namespace PaymentSystem.Application.QueryHandlers.PaymentQueryHandlers;

public class GetPaymentByIdQueryHandler(IPaymentRepository paymentRepository) : IRequestHandler<GetPaymentByIdQuery, Payment>
{
    private readonly IPaymentRepository _paymentRepository = paymentRepository;

    public async Task<Payment> Handle(GetPaymentByIdQuery request, CancellationToken cancellationToken) => await _paymentRepository.GetPaymentById(request.Id);
}
