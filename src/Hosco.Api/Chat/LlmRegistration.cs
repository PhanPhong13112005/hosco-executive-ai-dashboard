using Hosco.Application.Abstractions;
using Hosco.Application.Models;
using Hosco.Application.Services;

namespace Hosco.Api.Chat;

public static class LlmRegistration
{
    public static IServiceCollection AddChatLlm(this IServiceCollection services, IConfiguration configuration)
    {
        var options = LlmProviderOptions.FromConfiguration(key => configuration[key]);
        services.AddSingleton(options);
        services.AddSingleton<LlmIntentContract>();
        services.AddSingleton<DisabledLlmProvider>();
        services.AddSingleton<ChatIntentResolver>(sp => new ChatIntentResolver(sp.GetRequiredService<IBusinessTime>(),
            sp.GetRequiredService<TimeProvider>(), sp.GetRequiredService<DisabledLlmProvider>()));
        services.AddHttpClient<OpenAiLlmProvider>(client => client.Timeout = Timeout.InfiniteTimeSpan)
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false }).RemoveAllLoggers();
        services.AddHttpClient<GeminiLlmProvider>(client => client.Timeout = Timeout.InfiniteTimeSpan)
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false }).RemoveAllLoggers();
        services.AddScoped<ILlmProvider>(sp => !options.IsConfigured ? sp.GetRequiredService<DisabledLlmProvider>() : options.Provider switch
        {
            LlmProviderKind.OpenAI => sp.GetRequiredService<OpenAiLlmProvider>(),
            LlmProviderKind.Gemini => sp.GetRequiredService<GeminiLlmProvider>(),
            _ => sp.GetRequiredService<DisabledLlmProvider>()
        });
        services.AddScoped<IIntentResolver, LlmIntentResolver>();
        return services;
    }
}
