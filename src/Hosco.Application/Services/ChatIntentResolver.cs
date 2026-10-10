using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Hosco.Application.Abstractions;
using Hosco.Application.Models;

namespace Hosco.Application.Services;

public sealed partial class ChatIntentResolver(
    IBusinessTime businessTime,
    TimeProvider timeProvider,
    ILlmProvider llmProvider) : IIntentResolver
{
    public async Task<ChatIntentResult> ResolveAsync(string message, ChatConversationContext? context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (ResolveConversation(message) is { } conversation) return conversation;
        if (ResolveUnsupportedAnalysis(message) is { } unsupported) return unsupported;
        var text = Normalize(message);
        var dateRange = ResolveDateRange(text);
        var intent = ResolveIntent(text);
        if (intent == ChatIntent.Unknown && dateRange is not null && IsPeriodFollowUp(text) && context?.PreviousIntent is { Length: > 0 } previous &&
            Enum.TryParse<ChatIntent>(previous, true, out var contextualIntent) && Enum.IsDefined(contextualIntent) && contextualIntent != ChatIntent.Unknown)
            intent = contextualIntent;

        if (intent == ChatIntent.Unknown)
        {
            var providerResult = await llmProvider.TryResolveAsync(message, context, cancellationToken);
            return providerResult ?? new ChatIntentResult(ChatIntent.Unknown, ChatResolutionStatus.Unknown, 0m,
                Clarification: "Hiện tại tôi chưa hỗ trợ loại câu hỏi này.");
        }

        var branch = ResolveBranchReference(text);
        var limit = ResolveLimit(text, intent);
        var severity = intent == ChatIntent.CurrentAlerts ? ResolveSeverity(text) : null;
        var needsDate = intent is ChatIntent.KpiOverview or ChatIntent.Revenue or ChatIntent.Gmv or
            ChatIntent.TotalOrders or ChatIntent.Aov or ChatIntent.GrossProfit or ChatIntent.GrossMargin or
            ChatIntent.CancellationReturnRate or ChatIntent.RevenueTrend;
        if (needsDate && dateRange is null)
            return new ChatIntentResult(intent, ChatResolutionStatus.Ambiguous, .72m, BranchReference: branch,
                Limit: limit, Metric: Metric(intent), Severity: severity,
                Clarification: CalendarMonthRegex().IsMatch(text)
                    ? "Bạn muốn xem tháng nào, năm nào? Vui lòng ghi tháng 1–12 và năm, ví dụ: doanh thu tháng 8 năm 2026."
                    : "Bạn muốn xem hôm nay, hôm qua, tuần này, tuần trước, tháng này, tháng trước hay N ngày gần nhất?");

        return new ChatIntentResult(intent, ChatResolutionStatus.Resolved, .95m, dateRange, branch, limit,
            Metric(intent), severity);
    }

    // A recognized metric is not permission to silently discard unsupported dimensions.
    // Both local and LLM paths use this check; it never creates a new reporting query.
    internal static ChatIntentResult? ResolveUnsupportedAnalysis(string message)
    {
        var text = Normalize(message);
        var unsupported = ContainsAny(text, "nhan vien", "thu ngan", "khach hang", "tong thu", "tong chi", "chi phi",
            "so sanh", "so voi", "theo tung chi nhanh", "chi nhanh nao", "theo doanh thu", "sku-") || IncomeExpenseRegex().IsMatch(text);
        return unsupported ? new ChatIntentResult(ChatIntent.Unknown, ChatResolutionStatus.Unknown, 1m,
            Clarification: "Hiện chưa có Reporting API trong MVP cho chiều phân tích hoặc bộ lọc này. Tôi hỗ trợ KPI theo kỳ/chi nhánh, xếp hạng SKU theo số lượng, tồn kho nguy hiểm và cảnh báo.") : null;
    }

    // Local informational replies reuse the existing non-reporting contract.
    // Exact normalized phrases cannot turn a mixed business/security request into a greeting.
    internal static ChatIntentResult? ResolveConversation(string message)
    {
        var text = Normalize(message).TrimEnd('?', '!', '.', ' ');
        var reply = text switch
        {
            "hello" or "hi" or "xin chao" or "chao" =>
                "Xin chào! Tôi là trợ lý báo cáo HOSCO. Bạn muốn xem doanh thu, sản phẩm, tồn kho hay cảnh báo?",
            "help" or "ban lam duoc gi" or "ban co the lam gi" or "huong dan" =>
                "Tôi có thể tra cứu KPI, doanh thu, Top/Bottom sản phẩm, tồn kho nguy hiểm và cảnh báo trong phạm vi bạn được cấp quyền. " +
                "Hãy nêu chỉ số, thời gian và chi nhánh; ví dụ: Doanh thu hôm nay? Tôi không thực thi SQL hoặc thay đổi dữ liệu.",
            _ => null
        };
        return reply is null ? null : new ChatIntentResult(ChatIntent.Unknown, ChatResolutionStatus.Unknown, 1m,
            Clarification: reply);
    }

    private ChatDateRange? ResolveDateRange(string text)
    {
        text = text.Replace("thang ni", "thang nay", StringComparison.Ordinal);
        var today = businessTime.GetBusinessDate(timeProvider.GetUtcNow());
        if (text.Contains("hom nay")) return Range(today, today, "hôm nay");
        if (text.Contains("hom qua")) return Range(today.AddDays(-1), today.AddDays(-1), "hôm qua");
        if (text.Contains("tuan nay"))
        {
            var start = StartOfWeek(today);
            return Range(start, today, "tuần này");
        }
        if (text.Contains("tuan truoc"))
        {
            var thisWeek = StartOfWeek(today);
            return Range(thisWeek.AddDays(-7), thisWeek.AddDays(-1), "tuần trước");
        }
        if (text.Contains("thang nay"))
            return Range(new DateOnly(today.Year, today.Month, 1), today, "tháng này");
        if (text.Contains("thang truoc"))
        {
            var first = new DateOnly(today.Year, today.Month, 1).AddMonths(-1);
            return Range(first, first.AddMonths(1).AddDays(-1), "tháng trước");
        }
        var recent = RecentDaysRegex().Match(text);
        if (recent.Success && int.TryParse(recent.Groups[1].Value, out var days) && days is >= 1 and <= 366)
            return Range(today.AddDays(-(days - 1)), today, $"{days} ngày gần nhất");
        var month = CalendarMonthRegex().Match(text);
        if (month.Success && int.TryParse(month.Groups[1].Value, out var monthNumber) && monthNumber is >= 1 and <= 12 &&
            int.TryParse(month.Groups[2].Value, out var year) && year is >= 2000 and <= 2100)
        {
            var first = new DateOnly(year, monthNumber, 1);
            return Range(first, first.AddMonths(1).AddDays(-1), $"tháng {monthNumber} năm {year}");
        }
        return null;
    }

    private ChatDateRange Range(DateOnly from, DateOnly to, string label) => new(
        businessTime.StartOfBusinessDayUtc(from),
        businessTime.StartOfBusinessDayUtc(to.AddDays(1)).AddTicks(-1),
        label);

    private static DateOnly StartOfWeek(DateOnly date)
    {
        var offset = ((int)date.DayOfWeek + 6) % 7;
        return date.AddDays(-offset);
    }

    private static ChatIntent ResolveIntent(string text)
    {
        if ((text.Contains("ton kho") && text.Contains("nguy hiem")) || ContainsAny(text, "hang ton nguy hiem", "dangerous stock")) return ChatIntent.DangerousInventory;
        if (text.Contains("sap het hang")) return ChatIntent.DangerousInventory;
        if (ContainsAny(text, "canh bao", "alert")) return ChatIntent.CurrentAlerts;
        if (ContainsAny(text, "top ", "ban chay", "san pham top")) return ChatIntent.TopProducts;
        if (ContainsAny(text, "bottom", "ban cham", "xep cuoi", "ban it nhat", "doanh so thap nhat")) return ChatIntent.BottomProducts;
        if (ContainsAny(text, "ty le huy", "ty le hoan", "huy hoan")) return ChatIntent.CancellationReturnRate;
        if (ContainsAny(text, "bien loi nhuan", "gross margin")) return ChatIntent.GrossMargin;
        if (ContainsAny(text, "loi nhuan", "gross profit")) return ChatIntent.GrossProfit;
        if (ContainsAny(text, "gia tri don trung binh", "aov")) return ChatIntent.Aov;
        if (ContainsAny(text, "tong don", "so don", "total orders")) return ChatIntent.TotalOrders;
        if (text.Contains("gmv")) return ChatIntent.Gmv;
        if (ContainsAny(text, "tong quan", "kpi")) return ChatIntent.KpiOverview;
        if (ContainsAny(text, "doanh thu", "revenue") || RevenueAbbreviationRegex().IsMatch(text))
            return ContainsAny(text, "xu huong", "ngay gan nhat", "trend", "theo ngay") ? ChatIntent.RevenueTrend : ChatIntent.Revenue;
        return ChatIntent.Unknown;
    }

    private static int? ResolveLimit(string text, ChatIntent intent)
    {
        if (intent is not (ChatIntent.TopProducts or ChatIntent.BottomProducts or ChatIntent.DangerousInventory or ChatIntent.CurrentAlerts))
            return null;
        var match = LimitRegex().Match(text);
        return match.Success && int.TryParse(match.Groups[1].Value, out var value) ? Math.Clamp(value, 1, 20) : 5;
    }

    private static string? ResolveSeverity(string text)
    {
        if (text.Contains("critical")) return "Critical";
        if (text.Contains("high")) return "High";
        if (text.Contains("medium")) return "Medium";
        return null;
    }

    private static string? ResolveBranchReference(string text)
    {
        var match = BranchRegex().Match(text);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    private static string? Metric(ChatIntent intent) => intent switch
    {
        ChatIntent.Revenue or ChatIntent.RevenueTrend => "revenue",
        ChatIntent.Gmv => "gmv",
        ChatIntent.TotalOrders => "total-orders",
        ChatIntent.Aov => "aov",
        ChatIntent.GrossProfit => "gross-profit",
        ChatIntent.GrossMargin => "gross-margin",
        ChatIntent.CancellationReturnRate => "cancel-return-rate",
        ChatIntent.TopProducts or ChatIntent.BottomProducts => "sku-ranking",
        ChatIntent.DangerousInventory => "dangerous-stock",
        _ => null
    };

    internal static string Normalize(string value)
    {
        var decomposed = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var result = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                result.Append(character == 'đ' ? 'd' : character);
        return WhitespaceRegex().Replace(result.ToString().Normalize(NormalizationForm.FormC), " ");
    }

    private static bool ContainsAny(string text, params string[] values) => values.Any(text.Contains);

    private static bool IsPeriodFollowUp(string text) => PeriodFollowUpRegex().IsMatch(text);

    [GeneratedRegex(@"^(?:con\s+)?(?:hom nay|hom qua|tuan nay|tuan truoc|thang nay|thang truoc|\d{1,3}\s+ngay\s+gan\s+nhat)(?:\s+thi sao)?\s*[?.!]*$")]
    private static partial Regex PeriodFollowUpRegex();

    [GeneratedRegex(@"\b(\d{1,3})\s+ngay\s+gan\s+nhat\b")]
    private static partial Regex RecentDaysRegex();

    [GeneratedRegex(@"(?:top|bottom)?\s*(\d{1,3})\b")]
    private static partial Regex LimitRegex();

    [GeneratedRegex(@"(?:chi\s+nhanh\s+|cua\s+(?:chi\s+nhanh\s+)?(?!(?:toan|he thong)\b))(.+?)(?=\s+(?:hom\s+nay|hom\s+qua|tuan\s+nay|tuan\s+truoc|thang\s+\S+|\d+\s+ngay\s+gan\s+nhat|la\s+bao\s+nhieu|bao\s+nhieu)|[?.,]|$)")]
    private static partial Regex BranchRegex();

    [GeneratedRegex(@"\bdt\b")]
    private static partial Regex RevenueAbbreviationRegex();

    // Do not mistake "doanh thu chi nhánh ..." for the unsupported "thu chi" report.
    [GeneratedRegex(@"(?<!doanh )\bthu chi\b")]
    private static partial Regex IncomeExpenseRegex();

    [GeneratedRegex(@"\bthang\s+(\d{1,2})(?:\s+(?:nam\s+)?(\d{4}))?\b")]
    private static partial Regex CalendarMonthRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}

public sealed class DisabledLlmProvider : ILlmProvider
{
    public Task<ChatIntentResult?> TryResolveAsync(string message, ChatConversationContext? context,
        CancellationToken cancellationToken) => Task.FromResult<ChatIntentResult?>(null);
}
