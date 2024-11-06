namespace OrdersSystem.Domain.Exceptions;

public class OrderNotFoundException : Exception
{
    public OrderNotFoundException() : base("The order provided is not found!") { }

}
