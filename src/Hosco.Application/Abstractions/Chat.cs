using Hosco.Application.Models;

namespace Hosco.Application.Abstractions;

public interface IChatService
{
    Task<ChatServiceResult> SendAsync(ChatMessageRequest request, CancellationToken cancellationToken);
}

public interface IIntentResolver
{
    Task<ChatIntentResult> ResolveAsync(string message, ChatConversationContext? context,
        CancellationToken cancellationToken);
}

public interface IChatSafetyGuard
{
    void EnsureSafe(string message);
}

public interface IChatAuthorizationGuard
{
    ChatOperationRequest Authorize(ChatIntentResult intent, IReadOnlyList<ChatBranch> accessibleBranches);
}

public interface IReportingApiClient
{
    Task<IReadOnlyList<ChatBranch>> GetBranchesAsync(CancellationToken cancellationToken);
    Task<ReportingApiResult> ExecuteAsync(ChatOperationRequest request, CancellationToken cancellationToken);
}

public interface IResponseComposer
{
    string Compose(ChatIntentResult intent, ReportingApiResult result);
}

public interface ILlmProvider
{
    Task<ChatIntentResult?> TryResolveAsync(string message, ChatConversationContext? context,
        CancellationToken cancellationToken);
}
