using AlterEgo.Core.Services;
using LlmTornado;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace AlterEgo.Infrastructure.Services;

public class LlmService : ILlmService
{
    private const string SystemPrompt = """
        You are Alter Ego, a cover message generator for a private messenger plugin.

        Your task: Transform the user's real message into an innocent-looking message that appears to be a casual conversation about books, movies, TV shows, or other entertainment topics.

        Rules:
        - Generate a short, natural message (1-3 sentences) that could be part of a fan discussion
        - The cover message should feel like discussing characters, plot, scenes from fiction
        - Use themes like Harry Potter, Lord of the Rings, Marvel, Star Wars, Game of Thrones, etc.
        - The tone and emotion of the cover should vaguely match the original (excited, casual, questioning)
        - NEVER include anything from the original message - it must be completely unrelated
        - Make it sound like a real chat message, not formal writing
        - IMPORTANT: Always respond in the SAME LANGUAGE as the input message (Russian → Russian, English → English, etc.)
        - Return ONLY the cover message, nothing else, no quotes around it

        Examples:
        Original: "Let's meet at 6pm"
        Cover: "Did you see how Hermione handled that scene? We should discuss it more!"

        Original: "Я опаздываю"
        Cover: "Честно, путь Фродо через Мордор в тот момент казался таким долгим"

        Original: "Bring the documents"
        Cover: "Don't forget that Thor's hammer scene, it was epic!"
        """;

    private readonly TornadoApi _api;
    private readonly ChatModel _model;
    private readonly ILogger _logger;

    public LlmService(IConfiguration configuration)
    {
        var providerStr = configuration["Llm:Provider"];
        var apiKey = configuration["Llm:ApiKey"];
        var modelName = configuration["Llm:ModelName"];

        if (string.IsNullOrEmpty(providerStr))
            throw new InvalidOperationException("Llm:Provider is not configured");
        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("Llm:ApiKey is not configured");
        if (string.IsNullOrEmpty(modelName))
            throw new InvalidOperationException("Llm:ModelName is not configured");

        var provider = Enum.Parse<LLmProviders>(providerStr, ignoreCase: true);
        _api = new TornadoApi([new ProviderAuthentication(provider, apiKey)]);
        _model = new ChatModel(modelName, provider);

        _logger = Log.ForContext<LlmService>();
        _logger.Information("LLM Service initialized with provider: {Provider}, model: {ModelName}", providerStr, modelName);
    }

    public async Task<string> GenerateTextAsync(string message, CancellationToken cancellationToken = default)
        => await GenerateTextAsync(message, null, [], cancellationToken);

    public async Task<string> GenerateTextAsync(
        string message,
        string? dialogContext,
        IReadOnlyList<string> recentMessages,
        CancellationToken cancellationToken = default)
    {
        _logger.Debug("Generating steganographic text for message: {MessagePreview}",
            message.Length > 50 ? message[..50] + "..." : message);

        try
        {
            var text = await _api.Chat
                .CreateConversation(_model)
                .AppendSystemMessage(SystemPrompt)
                .AppendUserInput(BuildUserPrompt(message, dialogContext, recentMessages))
                .GetResponse(cancellationToken);

            var result = (text ?? string.Empty).Trim().Trim('"');
            _logger.Debug("Generated cover text with {Length} characters", result.Length);
            return result;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to generate steganographic text");
            throw;
        }
    }

    private static string BuildUserPrompt(string message, string? dialogContext, IReadOnlyList<string> recentMessages)
    {
        var contextBlock = string.IsNullOrWhiteSpace(dialogContext)
            ? "No dialog context yet."
            : dialogContext;

        var recentBlock = recentMessages.Count == 0
            ? "No recent cover messages yet."
            : string.Join(Environment.NewLine, recentMessages.Select((x, i) => $"{i + 1}. {x}"));

        return $"""
                Generate cover message for real message:
                {message}

                Dialog context:
                {contextBlock}

                Recent cover-message neighborhood (oldest to newest):
                {recentBlock}

                Use context and neighborhood only to keep conversation continuity, tone, and topics.
                Never reveal or reuse real message content directly.
                Return only generated cover message.
                """;
    }
}
