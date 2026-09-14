using Hosco.Infrastructure.Persistence;

namespace Hosco.Api.Observability;

public sealed class CorrelationContext(IHttpContextAccessor accessor) : ICorrelationContext
{
    public string CorrelationId => accessor.HttpContext?.Items[CorrelationMiddleware.ItemKey]?.ToString() ?? "unavailable";
}

public sealed class CorrelationMiddleware(RequestDelegate next, ILogger<CorrelationMiddleware> logger)
{
    public const string Header = "X-Correlation-ID";
    public const string ItemKey = "Hosco.CorrelationId";

    public async Task InvokeAsync(HttpContext context)
    {
        var incoming = context.Request.Headers[Header].FirstOrDefault();
        var correlationId = IsValid(incoming) ? incoming! : Guid.NewGuid().ToString("N");
        context.Items[ItemKey] = correlationId;
        context.Response.Headers[Header] = correlationId;
        var started = System.Diagnostics.Stopwatch.StartNew();
        try { await next(context); }
        finally
        {
            logger.LogInformation("HTTP {Path} responded {StatusCode} in {ElapsedMs}ms correlationId={CorrelationId} userId={UserId} tenantId={TenantId} branchScope={BranchScope} queryId={QueryId}",
                context.Request.Path, context.Response.StatusCode, started.ElapsedMilliseconds, correlationId,
                context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                context.User.FindFirst("tenant_id")?.Value,
                string.Join(',', context.User.FindAll("branch_id").Select(x => x.Value)),
                context.Items["Hosco.QueryId"]?.ToString());
        }
    }

    private static bool IsValid(string? value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 128 && value.All(c => char.IsLetterOrDigit(c) || c is '-' or '_' or '.');
}
