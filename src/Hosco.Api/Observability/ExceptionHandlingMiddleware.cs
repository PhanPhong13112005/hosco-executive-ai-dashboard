using Hosco.Application.Abstractions;

namespace Hosco.Api.Observability;

public sealed record ApiError(string Code, string Message, string CorrelationId);

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IWebHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (Exception exception)
        {
            var (status, code, message) = exception switch
            {
                ValidationException => (400, "validation_error", exception.Message),
                ForbiddenException => (403, "forbidden", exception.Message),
                KeyNotFoundException => (404, "not_found", exception.Message),
                BusinessDefinitionPendingException => (409, "business_definition_pending", exception.Message),
                OperationCanceledException when context.RequestAborted.IsCancellationRequested => (499, "request_cancelled", "The request was cancelled."),
                _ => (500, "internal_error", environment.IsDevelopment() ? exception.Message : "An unexpected error occurred.")
            };
            var correlationId = context.Items[CorrelationMiddleware.ItemKey]?.ToString() ?? "unavailable";
            logger.LogError(exception, "Request failed code={ErrorCode} correlationId={CorrelationId}", code, correlationId);
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new ApiError(code, message, correlationId));
        }
    }
}
