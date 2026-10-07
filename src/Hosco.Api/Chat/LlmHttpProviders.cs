using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Hosco.Application.Abstractions;
using Hosco.Application.Models;
using Hosco.Application.Services;

namespace Hosco.Api.Chat;

public abstract class LlmHttpProvider(HttpClient client, LlmProviderOptions options, LlmIntentContract contract) : ILlmProvider
{
    protected abstract HttpRequestMessage CreateRequest(string message, ChatConversationContext? context);
    protected abstract string? Extract(JsonElement root);
    protected LlmProviderOptions Options => options;
    protected LlmIntentContract Contract => contract;

    public async Task<ChatIntentResult?> TryResolveAsync(string message, ChatConversationContext? context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        new ChatSafetyGuard().EnsureSafe(message);
        if (!options.IsConfigured) return null;
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        budget.CancelAfter(options.Timeout); // Total budget, including retries, backoff and response reads.
        try
        {
            for (var attempt = 0; attempt <= options.MaxRetries; attempt++)
            {
                try
                {
                    using var request = CreateRequest(message, LlmIntentContract.SafeContext(context));
                    using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, budget.Token);
                    if ((response.StatusCode == HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500) && attempt < options.MaxRetries)
                    { await Task.Delay(100 * (attempt + 1), budget.Token); continue; }
                    if (!response.IsSuccessStatusCode) return null;
                    await using var stream = await response.Content.ReadAsStreamAsync(budget.Token);
                    using var buffer = new MemoryStream();
                    var chunk = new byte[4096];
                    int count;
                    while ((count = await stream.ReadAsync(chunk, budget.Token)) != 0)
                    {
                        if (buffer.Length + count > 65536) return null;
                        buffer.Write(chunk, 0, count);
                    }
                    using var json = JsonDocument.Parse(buffer.ToArray(), new JsonDocumentOptions { MaxDepth = 32 });
                    var output = Extract(json.RootElement);
                    return output is null ? null : contract.Parse(output);
                }
                catch (HttpRequestException) when (attempt < options.MaxRetries)
                { await Task.Delay(100 * (attempt + 1), budget.Token); }
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { return null; }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException or IOException or KeyNotFoundException or FormatException)
        { return null; } // Never surface provider body, credentials, raw prompt or network exception.
        cancellationToken.ThrowIfCancellationRequested();
        return null;
    }

    protected static string UserInput(string message, ChatConversationContext? context) => JsonSerializer.Serialize(new { message, context });
}

public sealed class OpenAiLlmProvider(HttpClient client, LlmProviderOptions options, LlmIntentContract contract)
    : LlmHttpProvider(client, options, contract)
{
    protected override HttpRequestMessage CreateRequest(string message, ChatConversationContext? context)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Options.ApiKey);
        request.Content = JsonContent.Create(new
        {
            model = Options.Model, store = false, max_output_tokens = 2048,
            input = new[] { new { role = "system", content = Contract.SystemPrompt }, new { role = "user", content = UserInput(message, context) } },
            text = new { format = new { type = "json_schema", name = "hosco_intent", strict = true, schema = JsonSerializer.Deserialize<JsonElement>(LlmIntentContract.Schema) } }
        });
        return request;
    }

    protected override string? Extract(JsonElement root)
    {
        if (root.GetProperty("status").GetString() != "completed") return null;
        var texts = new List<string>();
        foreach (var item in root.GetProperty("output").EnumerateArray())
        {
            if (item.GetProperty("type").GetString() != "message") continue;
            if (item.GetProperty("role").GetString() != "assistant" || item.GetProperty("status").GetString() != "completed") return null;
            foreach (var content in item.GetProperty("content").EnumerateArray())
            {
                if (content.GetProperty("type").GetString() != "output_text") return null; // Includes refusals.
                texts.Add(content.GetProperty("text").GetString()!);
            }
        }
        return texts.Count == 1 ? texts[0] : null;
    }
}

public sealed class GeminiLlmProvider(HttpClient client, LlmProviderOptions options, LlmIntentContract contract)
    : LlmHttpProvider(client, options, contract)
{
    protected override HttpRequestMessage CreateRequest(string message, ChatConversationContext? context)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"https://generativelanguage.googleapis.com/v1beta/models/{Options.Model}:generateContent");
        request.Headers.Add("x-goog-api-key", Options.ApiKey);
        request.Content = JsonContent.Create(new
        {
            systemInstruction = new { parts = new[] { new { text = Contract.SystemPrompt } } },
            contents = new[] { new { role = "user", parts = new[] { new { text = UserInput(message, context) } } } },
            generationConfig = new { responseMimeType = "application/json", responseJsonSchema = JsonSerializer.Deserialize<JsonElement>(LlmIntentContract.Schema), maxOutputTokens = 2048 }
        });
        return request;
    }

    protected override string? Extract(JsonElement root)
    {
        if (root.TryGetProperty("promptFeedback", out var feedback) && feedback.TryGetProperty("blockReason", out _)) return null;
        var candidates = root.GetProperty("candidates");
        if (candidates.GetArrayLength() != 1 || candidates[0].GetProperty("finishReason").GetString() != "STOP") return null;
        var parts = candidates[0].GetProperty("content").GetProperty("parts");
        if (parts.GetArrayLength() != 1 || parts[0].TryGetProperty("functionCall", out _) ||
            (parts[0].TryGetProperty("thought", out var thought) && thought.GetBoolean())) return null;
        return parts[0].GetProperty("text").GetString();
    }
}
