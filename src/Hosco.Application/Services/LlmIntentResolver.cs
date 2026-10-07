using Hosco.Application.Abstractions;
using Hosco.Application.Models;

namespace Hosco.Application.Services;

// Keep the existing deterministic resolver intact, including its supported periods/follow-ups.
public sealed class LlmIntentResolver(ILlmProvider provider, ChatIntentResolver deterministic, IChatSafetyGuard safety) : IIntentResolver
{
    public async Task<ChatIntentResult> ResolveAsync(string message, ChatConversationContext? context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        safety.EnsureSafe(message);
        if (ChatIntentResolver.ResolveConversation(message) is { } conversation) return conversation;
        var safeContext = LlmIntentContract.SafeContext(context);
        ChatIntentResult? result;
        try { result = await provider.TryResolveAsync(message, safeContext, cancellationToken); }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { result = null; }
        catch (Exception ex) when (ex is HttpRequestException or TimeoutException) { result = null; }
        cancellationToken.ThrowIfCancellationRequested();
        if (result is null) return await deterministic.ResolveAsync(message, safeContext, cancellationToken);
        if (result.Status == ChatResolutionStatus.Resolved && result.Confidence < .75m)
            return result with { Status = ChatResolutionStatus.Ambiguous, Clarification = "Vui lòng làm rõ câu hỏi bạn muốn xem." };
        return result;
    }
}
