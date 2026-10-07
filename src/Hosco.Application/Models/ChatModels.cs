using System.Text.Json;

namespace Hosco.Application.Models;

public enum ChatIntent
{
    Unknown,
    KpiOverview,
    Revenue,
    Gmv,
    TotalOrders,
    Aov,
    GrossProfit,
    GrossMargin,
    CancellationReturnRate,
    RevenueTrend,
    TopProducts,
    BottomProducts,
    DangerousInventory,
    CurrentAlerts
}

public enum ChatResolutionStatus { Resolved, Ambiguous, Unknown }

public enum ReportingOperation
{
    DashboardSummary,
    RevenueTrend,
    TopProducts,
    BottomProducts,
    DangerousInventory,
    AlertList,
    Branches
}

public sealed record ChatConversationContext(string? PreviousIntent = null, string? PreviousMetric = null);

public sealed record ChatMessageRequest(string Message, ChatConversationContext? Context = null);

public sealed record ChatDateRange(DateTimeOffset From, DateTimeOffset To, string Label);

public sealed record ChatIntentResult(
    ChatIntent Intent,
    ChatResolutionStatus Status,
    decimal Confidence,
    ChatDateRange? DateRange = null,
    string? BranchReference = null,
    int? Limit = null,
    string? Metric = null,
    string? Severity = null,
    string? Clarification = null,
    IReadOnlyList<string>? ValidationErrors = null);

public sealed record ChatBranch(Guid Id, string Code, string Name);

public sealed record ChatOperationRequest(
    ReportingOperation Operation,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    Guid? BranchId = null,
    int? Limit = null,
    string? Severity = null);

public sealed record ReportingApiResult(JsonElement Data, string QueryId, bool IsStale = false);

public sealed record ChatServiceResult(
    string Message,
    string Intent,
    string Status,
    decimal Confidence,
    JsonElement? Data,
    IReadOnlyList<string> Suggestions,
    ChatConversationContext Context,
    string? ReportingOperation = null);

public sealed record ChatMessageResponse(
    string Message,
    string Intent,
    string Status,
    decimal Confidence,
    JsonElement? Data,
    IReadOnlyList<string> Suggestions,
    ChatConversationContext Context,
    string? ReportingOperation,
    string CorrelationId);

