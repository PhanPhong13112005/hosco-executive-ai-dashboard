using Hosco.Application.Abstractions;
using Hosco.Application.Models;

namespace Hosco.Application.Services;

public sealed class ChatService(
    IChatSafetyGuard safety,
    IIntentResolver resolver,
    IChatAuthorizationGuard authorization,
    IReportingApiClient reporting,
    IResponseComposer composer,
    IAuditWriter audit) : IChatService
{
    private static readonly string[] DefaultSuggestions =
    [
        "Doanh thu hôm nay?", "Top 5 sản phẩm bán chạy?", "Tồn kho nào đang nguy hiểm?",
        "Có cảnh báo Critical nào không?", "Doanh thu 7 ngày gần nhất?"
    ];

    public async Task<ChatServiceResult> SendAsync(ChatMessageRequest request, CancellationToken cancellationToken)
    {
        safety.EnsureSafe(request.Message);
        var intent = await resolver.ResolveAsync(request.Message, request.Context, cancellationToken);
        var context = new ChatConversationContext(intent.Intent.ToString(), intent.Metric);
        if (intent.Status != ChatResolutionStatus.Resolved)
        {
            await audit.WriteAsync("chat.resolve", "Chat", null, null, null,
                new { intent = intent.Intent.ToString(), status = intent.Status.ToString() }, cancellationToken);
            return new ChatServiceResult(intent.Clarification ?? "Vui lòng làm rõ câu hỏi.", intent.Intent.ToString(),
                intent.Status.ToString(), intent.Confidence, null, DefaultSuggestions, context);
        }

        var branches = string.IsNullOrWhiteSpace(intent.BranchReference)
            ? Array.Empty<ChatBranch>()
            : await reporting.GetBranchesAsync(cancellationToken);
        var operation = authorization.Authorize(intent, branches);
        try
        {
            var result = await reporting.ExecuteAsync(operation, cancellationToken);
            var message = composer.Compose(intent, result);
            await audit.WriteAsync("chat.query", "Reporting", null, result.QueryId, operation.BranchId,
                new { intent = intent.Intent.ToString(), operation = operation.Operation.ToString(), outcome = "success" }, cancellationToken);
            return new ChatServiceResult(message, intent.Intent.ToString(), "Completed", intent.Confidence,
                result.Data, DefaultSuggestions, context, operation.Operation.ToString());
        }
        catch (ReportingApiUnavailableException)
        {
            await audit.WriteAsync("chat.query", "Reporting", null, null, operation.BranchId,
                new { intent = intent.Intent.ToString(), operation = operation.Operation.ToString(), outcome = "unavailable" }, cancellationToken);
            return new ChatServiceResult("Hiện chưa thể lấy dữ liệu báo cáo. Vui lòng thử lại sau.",
                intent.Intent.ToString(), "Unavailable", intent.Confidence, null, DefaultSuggestions, context,
                operation.Operation.ToString());
        }
    }
}
