using System.Text.RegularExpressions;

namespace Hosco.Application.Models;

public enum LlmProviderKind { Mock, OpenAI, Gemini }

// Secrets are read by the host; never serialize or log this object.
public sealed class LlmProviderOptions
{
    public LlmProviderKind Provider { get; init; } = LlmProviderKind.Mock;
    public string Model { get; init; } = "";
    public string ApiKey { get; init; } = "";
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(5);
    public int MaxRetries { get; init; } = 1;
    public bool IsConfigured => Provider != LlmProviderKind.Mock && Model.Length > 0 && !string.IsNullOrWhiteSpace(ApiKey);

    public static LlmProviderOptions FromConfiguration(Func<string, string?> read)
    {
        var name = read("AI_PROVIDER") ?? "Mock";
        if (!Enum.TryParse<LlmProviderKind>(name, true, out var provider) || !Enum.IsDefined(provider))
            throw new InvalidOperationException("AI_PROVIDER must be Mock, OpenAI or Gemini.");
        var model = read("AI_MODEL")?.Trim() ?? "";
        if (model.Length > 100 || (model.Length > 0 && !Regex.IsMatch(model, @"^[a-zA-Z0-9._-]+$")))
            throw new InvalidOperationException("AI_MODEL must be a model identifier, not a URL.");
        var timeout = read("AI_TIMEOUT_SECONDS") ?? "5";
        var retries = read("AI_MAX_RETRIES") ?? "1";
        if (!int.TryParse(timeout, out var seconds) || seconds is < 1 or > 10)
            throw new InvalidOperationException("AI_TIMEOUT_SECONDS must be between 1 and 10.");
        if (!int.TryParse(retries, out var count) || count is < 0 or > 1)
            throw new InvalidOperationException("AI_MAX_RETRIES must be 0 or 1.");
        return new LlmProviderOptions
        {
            Provider = provider, Model = model,
            ApiKey = provider switch
            {
                LlmProviderKind.OpenAI => read("OPENAI_API_KEY") ?? "",
                LlmProviderKind.Gemini => read("GEMINI_API_KEY") ?? "",
                _ => ""
            },
            Timeout = TimeSpan.FromSeconds(seconds), MaxRetries = count
        };
    }
}
