using Hosco.Application.Abstractions;
using Hosco.Application.Models;
using Hosco.Application.Semantics;

namespace Hosco.Application.Services;

public sealed class ChatSafetyGuard : IChatSafetyGuard
{
    private const int MaxMessageLength = 1000;
    private static readonly string[] BlockedPatterns =
    [
        "ignore previous", "bo qua chi thi", "bo qua quyen", "vuot quyen", "all tenants", "tenant khac",
        "branchid", "branch id", "viet sql", "select *", "drop table", "insert into", "update orders",
        "connection string", "api key", "system prompt", "doc secret", "lay secret", "shell command",
        "thuc thi lenh", "command he thong", "goi url", "http://", "https://"
    ];

    public void EnsureSafe(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) throw new ValidationException("message is required.");
        if (message.Length > MaxMessageLength)
            throw new ValidationException($"message must not exceed {MaxMessageLength} characters.");
        var normalized = ChatIntentResolver.Normalize(message);
        if (BlockedPatterns.Any(normalized.Contains))
            throw new ForbiddenException("Yêu cầu này không được phép đối với trợ lý báo cáo chỉ đọc.");
    }
}

public sealed class ChatAuthorizationGuard(IQueryCatalog queryCatalog) : IChatAuthorizationGuard
{
    public ChatOperationRequest Authorize(ChatIntentResult intent, IReadOnlyList<ChatBranch> accessibleBranches)
    {
        if (intent.Status != ChatResolutionStatus.Resolved)
            throw new ValidationException("Only a resolved intent can execute a reporting operation.");

        var (operation, queryId) = intent.Intent switch
        {
            ChatIntent.KpiOverview or ChatIntent.Revenue or ChatIntent.Gmv or ChatIntent.TotalOrders or
                ChatIntent.Aov or ChatIntent.GrossProfit or ChatIntent.GrossMargin or
                ChatIntent.CancellationReturnRate => (ReportingOperation.DashboardSummary, "dashboard.summary.v1"),
            ChatIntent.RevenueTrend => (ReportingOperation.RevenueTrend, "revenue.trend.v1"),
            ChatIntent.TopProducts => (ReportingOperation.TopProducts, "products.ranking.v1"),
            ChatIntent.BottomProducts => (ReportingOperation.BottomProducts, "products.ranking.v1"),
            ChatIntent.DangerousInventory => (ReportingOperation.DangerousInventory, "inventory.dangerous.v1"),
            ChatIntent.CurrentAlerts => (ReportingOperation.AlertList, "alerts.list.v1"),
            _ => throw new ValidationException("The resolved intent has no allow-listed reporting operation.")
        };
        var definition = queryCatalog.Get(queryId);
        if (definition.Status != DefinitionStatus.Implemented)
            throw new BusinessDefinitionPendingException(definition.MetricCode ?? definition.QueryId);

        Guid? branchId = null;
        if (!string.IsNullOrWhiteSpace(intent.BranchReference))
        {
            var reference = ChatIntentResolver.Normalize(intent.BranchReference);
            var matches = accessibleBranches.Where(x =>
                ChatIntentResolver.Normalize(x.Code).Equals(reference, StringComparison.OrdinalIgnoreCase) ||
                ChatIntentResolver.Normalize(x.Name).Equals(reference, StringComparison.OrdinalIgnoreCase)).ToList();
            if (matches.Count == 0)
                throw new ForbiddenException("Bạn không có quyền xem chi nhánh được yêu cầu.");
            if (matches.Count > 1)
                throw new ValidationException("Tên chi nhánh chưa đủ rõ. Vui lòng dùng đúng mã chi nhánh.");
            branchId = matches[0].Id;
        }

        return new ChatOperationRequest(operation, intent.DateRange?.From, intent.DateRange?.To, branchId,
            intent.Limit, intent.Severity);
    }
}
