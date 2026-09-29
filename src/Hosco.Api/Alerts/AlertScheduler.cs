using Hosco.Application.Abstractions;
using Hosco.Domain.Entities;
using Hosco.Application.Services;
using Microsoft.Extensions.Options;

namespace Hosco.Api.Alerts;

public sealed class AlertSchedulerOptions
{
    public const string Section = "AlertScheduler";
    public bool Enabled { get; set; } = true;
    public int IntervalMinutes { get; set; } = 15;
    public bool RunOnStartup { get; set; }
}

public sealed class LoggingAlertEngineDiagnostics(ILogger<LoggingAlertEngineDiagnostics> logger) : IAlertEngineDiagnostics
{
    public void RuleFailed(AlertRule rule, Exception exception) => logger.LogError(exception,
        "Alert rule evaluation failed ruleId={RuleId} ruleCode={RuleCode} tenantId={TenantId} branchId={BranchId}; remaining rules will continue",
        rule.Id, rule.Code, rule.TenantId, rule.BranchId);

    public void NotificationRetryFailed(Exception exception) => logger.LogError(exception,
        "Notification dispatch failed; the scheduler will continue and retry pending deliveries on a later cycle.");
}

public sealed class AlertSchedulerBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<AlertSchedulerOptions> options,
    ILogger<AlertSchedulerBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var configuration = options.Value;
        if (!configuration.Enabled)
        {
            logger.LogInformation("Alert scheduler is disabled.");
            return;
        }
        if (configuration.IntervalMinutes is < 15 or > 30)
            throw new InvalidOperationException("AlertScheduler:IntervalMinutes must be between 15 and 30.");

        logger.LogInformation("Alert scheduler started intervalMinutes={IntervalMinutes} runOnStartup={RunOnStartup}",
            configuration.IntervalMinutes, configuration.RunOnStartup);

        if (configuration.RunOnStartup)
            await EvaluateAsync(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(configuration.IntervalMinutes));
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await EvaluateAsync(stoppingToken);
    }

    private async Task EvaluateAsync(CancellationToken cancellationToken)
    {
        var correlationId = $"scheduler-{Guid.NewGuid():N}";
        using var logScope = logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId });
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var result = await scope.ServiceProvider.GetRequiredService<IAlertEngine>().EvaluateAllAsync(cancellationToken);
            logger.LogInformation(
                "Alert evaluation completed correlationId={CorrelationId} evaluatedRules={EvaluatedRules} createdAlerts={CreatedAlerts} suppressedDuplicates={SuppressedDuplicates} failedRules={FailedRules}",
                correlationId, result.EvaluatedRules, result.CreatedAlerts, result.SuppressedDuplicates, result.FailedRules);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Alert scheduler cycle failed correlationId={CorrelationId}; the worker will continue on the next configured interval.", correlationId);
        }
    }
}
