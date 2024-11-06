using System;

namespace PaymentSystem.Domain.Exceptions;

public class PaymentNotFoundException : Exception
{
    public PaymentNotFoundException() : base("Payment provided does not exist!") { }

}
