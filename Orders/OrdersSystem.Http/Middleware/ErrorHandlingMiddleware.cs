using OrdersSystem.Domain.Exceptions;

namespace OrdersSystem.Http.Middleware;

public class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
{
    private readonly ILogger<ErrorHandlingMiddleware> _logger = logger;
    private readonly RequestDelegate _next = next;

    public async Task InvokeAsync(HttpContext context)
    {

        try
        {
            await _next(context);
        }
        catch (OrderNotFoundException ex)
        {
            _logger.LogError(ex, "The provided order does not exist");
            await HandleExceptionAsync(context, StatusCodes.Status404NotFound, ex.Message);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred.");
            await HandleExceptionAsync(context, StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
        }
    }


    public static Task HandleExceptionAsync(HttpContext context, int statusCode, string message)
    {

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;
        return context.Response.WriteAsJsonAsync(new
        {
            message
        });
    }

}