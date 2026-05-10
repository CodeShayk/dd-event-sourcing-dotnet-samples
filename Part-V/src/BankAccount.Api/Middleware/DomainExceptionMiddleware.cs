// File: BankAccount.Api/Middleware/DomainExceptionMiddleware.cs
using BankAccount.Domain.Exceptions;

namespace BankAccount.Api.Middleware;

/// <summary>
/// Global exception handler middleware. Converts domain exceptions to
/// structured 400 Bad Request responses. All other exceptions propagate
/// to the default ASP.NET Core error handler (returning 500).
/// </summary>
public sealed class DomainExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DomainExceptionMiddleware> _logger;

    /// <summary>
    /// Initialises the middleware.
    /// </summary>
    public DomainExceptionMiddleware(
        RequestDelegate next,
        ILogger<DomainExceptionMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware pipeline, catching domain exceptions and
    /// writing a structured 400 response.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation: {Message}", ex.Message);

            context.Response.StatusCode  = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(new
            {
                error   = "Domain rule violation",
                message = ex.Message,
                code    = ex.ErrorCode
            });
        }
    }
}

/// <summary>Extension methods for DomainExceptionMiddleware registration.</summary>
public static class DomainExceptionMiddlewareExtensions
{
    /// <summary>
    /// Adds the DomainExceptionMiddleware to the ASP.NET Core pipeline.
    /// Register this BEFORE other middleware — specifically before
    /// UseAuthorization() and UseRouting(). If a domain exception is thrown
    /// during request processing and the domain exception handler has not yet
    /// been registered in the pipeline, it will propagate uncaught and the
    /// default error handler will return a 500 Internal Server Error instead
    /// of the intended 400 Bad Request. Position matters.
    /// </summary>
    public static IApplicationBuilder UseDomainExceptionHandler(
        this IApplicationBuilder app)
        => app.UseMiddleware<DomainExceptionMiddleware>();
}
