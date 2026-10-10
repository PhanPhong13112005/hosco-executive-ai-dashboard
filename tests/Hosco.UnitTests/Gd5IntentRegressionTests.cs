using Hosco.Application.Abstractions;
using Hosco.Application.Models;
using Hosco.Application.Semantics;
using Hosco.Application.Services;

namespace Hosco.UnitTests;

public sealed class Gd5IntentRegressionTests
{
    private sealed class Clock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 10, 10, 3, 0, 0, TimeSpan.Zero);
    }
    private static ChatIntentResolver Resolver() => new(new VietnamBusinessTime(), new Clock(), new DisabledLlmProvider());

    [Theory]
    [InlineData("Còn hôm qua thì sao?", "hôm qua")]
    [InlineData("Còn tháng trước thì sao?", "tháng trước")]
    [InlineData("Con hom qua thi sao?", "hôm qua")]
    public async Task Period_follow_up_inherits_only_valid_intent_with_context(string message, string label)
    {
        var result = await Resolver().ResolveAsync(message, new("Revenue", "untrusted-metric"), default);
        Assert.Equal(ChatIntent.Revenue, result.Intent);
        Assert.Equal(ChatResolutionStatus.Resolved, result.Status);
        Assert.Equal(label, result.DateRange!.Label);
        Assert.Equal("revenue", result.Metric);
        Assert.Null(result.BranchReference);
        Assert.Equal(ChatResolutionStatus.Unknown, (await Resolver().ResolveAsync(message, null, default)).Status);
    }

    [Theory]
    [InlineData("Xem dt thang 8")]
    [InlineData("Doanh thu tháng 13 năm 2026")]
    [InlineData("Doanh thu tháng 8 năm 9999")]
    public async Task Missing_year_or_invalid_month_asks_for_clarification_not_invented_period(string message)
    {
        var result = await Resolver().ResolveAsync(message, null, default);
        Assert.Equal(ChatIntent.Revenue, result.Intent);
        Assert.Equal(ChatResolutionStatus.Ambiguous, result.Status);
        Assert.Null(result.DateRange);
        Assert.Contains("năm", result.Clarification);
    }

    [Fact]
    public async Task Explicit_calendar_month_has_exact_utc_plus_seven_boundaries()
    {
        var result = await Resolver().ResolveAsync("Xem dt thang 2 nam 2024", null, default);
        Assert.Equal(ChatResolutionStatus.Resolved, result.Status);
        Assert.Equal(new DateTimeOffset(2024, 1, 31, 17, 0, 0, TimeSpan.Zero), result.DateRange!.From);
        Assert.Equal(new DateTimeOffset(2024, 2, 29, 17, 0, 0, TimeSpan.Zero).AddTicks(-1), result.DateRange.To);
    }

    [Theory]
    [InlineData("Doanh thu tháng nì của HN bao nhiêu?", ChatIntent.Revenue, "hn")]
    [InlineData("Doanh thu tháng trước của Hà Nội?", ChatIntent.Revenue, "ha noi")]
    [InlineData("Doanh thu tháng này của chi nhánh Hà Nội là bao nhiêu?", ChatIntent.Revenue, "ha noi")]
    [InlineData("Doanh thu chi nhánh Hà Nội tuần này bao nhiêu?", ChatIntent.Revenue, "ha noi")]
    [InlineData("Cho tôi doanh thu theo ngày trong tuần này.", ChatIntent.RevenueTrend, null)]
    [InlineData("SKU nào đang sắp hết hàng?", ChatIntent.DangerousInventory, null)]
    [InlineData("Cho tôi 5 SKU bán ít nhất tuần này.", ChatIntent.BottomProducts, null)]
    public async Task Supported_variants_preserve_intent_and_branch_reference(string message, ChatIntent intent, string? branch)
    {
        var result = await Resolver().ResolveAsync(message, null, default);
        Assert.Equal(intent, result.Intent);
        Assert.Equal(ChatResolutionStatus.Resolved, result.Status);
        Assert.Equal(branch, result.BranchReference);
    }

    [Theory]
    [InlineData("Top 5 nhân viên theo doanh thu tháng này.")]
    [InlineData("Nhân viên nào có doanh thu cao nhất tháng này?")]
    [InlineData("So sánh doanh thu tháng này với tháng trước.")]
    [InlineData("Doanh thu tháng này theo từng chi nhánh.")]
    [InlineData("SKU-A có đang ở mức tồn kho nguy hiểm không?")]
    [InlineData("Top sản phẩm theo doanh thu tháng này là gì?")]
    [InlineData("Thu chi tháng này thế nào?")]
    public async Task Unsupported_dimensions_are_not_silently_dropped_in_local_or_llm_path(string message)
    {
        var local = await Resolver().ResolveAsync(message, null, default);
        var provider = new ProviderSpy();
        var llm = await new LlmIntentResolver(provider, Resolver(), new ChatSafetyGuard()).ResolveAsync(message, null, default);
        Assert.Equal(ChatResolutionStatus.Unknown, local.Status);
        Assert.Equal(ChatResolutionStatus.Unknown, llm.Status);
        Assert.Contains("Reporting API", llm.Clarification);
        Assert.Equal(0, provider.Calls);
    }

    [Theory]
    [InlineData("HN")]
    [InlineData("Hà Nội")]
    public void Branch_alias_is_resolved_only_from_current_accessible_directory(string reference)
    {
        var branch = new ChatBranch(Guid.NewGuid(), "A-HN", "HOSCO Hà Nội");
        var result = new ChatIntentResult(ChatIntent.TopProducts, ChatResolutionStatus.Resolved, 1m, BranchReference: reference, Limit: 5);
        var guard = new ChatAuthorizationGuard(new QueryCatalog());
        Assert.Equal(branch.Id, guard.Authorize(result, [branch]).BranchId);
        Assert.Throws<ForbiddenException>(() => guard.Authorize(result, []));
    }

    private sealed class ProviderSpy : ILlmProvider
    {
        public int Calls { get; private set; }
        public Task<ChatIntentResult?> TryResolveAsync(string message, ChatConversationContext? context, CancellationToken ct)
        {
            Calls++;
            return Task.FromResult<ChatIntentResult?>(new(ChatIntent.Revenue, ChatResolutionStatus.Resolved, 1m));
        }
    }
}
