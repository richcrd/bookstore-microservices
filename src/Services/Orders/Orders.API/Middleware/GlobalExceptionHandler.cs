using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Orders.Domain.Exceptions;

namespace Orders.API.Middleware;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, title, type) = exception switch
        {
            OrderNotFoundException => (
                StatusCodes.Status404NotFound,
                "Resource not found",
                "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.5"),

            OrderDomainException => (
                StatusCodes.Status400BadRequest,
                "Business rule violation",
                "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.1"),

            _ => (
                StatusCodes.Status500InternalServerError,
                "An error occurred while processing your request",
                "https://datatracker.ietf.org/doc/html/rfc9110#section-15.6.1")
        };
        
        if (statusCode == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception for {Method}{Path}", httpContext.Request.Method,
                httpContext.Request.Path);
        else
            logger.LogWarning(exception, "Request failed for {Method}{Path}", httpContext.Request.Method, httpContext.Request.Path);

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails()
        {
            Type = type,
            Title = title,
            Status = statusCode,
            Detail = statusCode == StatusCodes.Status500InternalServerError ? null : exception.Message,
            Instance = httpContext.Request.Path,
        }, cancellationToken);

        return true;
    }
}
