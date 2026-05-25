using AI.Integration.Configuration;
using Microsoft.Extensions.Options;
using OpenAI;
using System.ClientModel;
using OpenAI.Chat;

namespace AI.Integration.Services;

public class LlmService
{
    private readonly OpenAIClient _client;
    private readonly string _modelName;
    private readonly ChatClient _chatClient;

    // IOptions pozwala na "wstrzyknięcie" danych z appsettings.json
    public LlmService(IOptions<LmStudioOptions> options)
    {
        var config = options.Value;

        var clientOptions = new OpenAIClientOptions
        {
            Endpoint = new Uri(config.BaseUrl)
        };

        var client = new OpenAIClient(new System.ClientModel.ApiKeyCredential(config.ApiKey), clientOptions);

        // Inicjalizujemy klienta czatu dla konkretnego modelu
        _chatClient = client.GetChatClient(config.ModelName);
    }

    // Prosta metoda testowa, która wyśle jedno zdanie do modelu
    public async Task<string> TestConnectionAsync(string message)
    {
        ChatCompletion completion = await _chatClient.CompleteChatAsync(message);
        return completion.Content[0].Text;
    }
}