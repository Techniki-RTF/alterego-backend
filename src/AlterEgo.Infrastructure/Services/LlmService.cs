using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AlterEgo.Core.Services;
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
        - Return ONLY the cover message, nothing else
        
        Examples:
        Original: "Let's meet at 6pm"
        Cover: "Did you see how Hermione handled that scene? We should discuss it more!"
        
        Original: "Я опаздываю"
        Cover: "Честно, путь Фродо через Мордор в тот момент казался таким долгим"
        
        Original: "Bring the documents"
        Cover: "Don't forget that Thor's hammer scene, it was epic!"
        """;

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _modelName;
    private readonly ILogger _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public LlmService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _apiKey = configuration["Gemini:ApiKey"]
            ?? throw new InvalidOperationException("Gemini:ApiKey is not configured");
        _modelName = configuration["Gemini:ModelName"] ?? "gemini-3-flash-preview";
        _httpClient = httpClientFactory.CreateClient("Gemini");

        _logger = Log.ForContext<LlmService>();
        _logger.Information("LLM Service initialized with model: {ModelName}", _modelName);
    }

    public async Task<string> GenerateTextAsync(string message, CancellationToken cancellationToken = default)
    {
        return await GenerateTextAsync(message, null, [], cancellationToken);
    }

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
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_modelName}:generateContent?key={_apiKey}";

            var request = new GeminiRequest
            {
                SystemInstruction = new GeminiContent
                {
                    Parts = [new GeminiPart { Text = SystemPrompt }]
                },
                Contents =
                [
                    new GeminiContent
                    {
                        Parts = [new GeminiPart { Text = BuildUserPrompt(message, dialogContext, recentMessages) }]
                    }
                ]
            };

            var response = await _httpClient.PostAsJsonAsync(url, request, JsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<GeminiResponse>(JsonOptions, cancellationToken);
            var text = result?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? string.Empty;

            _logger.Debug("Generated cover text with {Length} characters", text.Length);
            return text;
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
                "{message}"

                Dialog context:
                {contextBlock}

                Recent cover-message neighborhood (oldest to newest):
                {recentBlock}

                Use context and neighborhood only to keep conversation continuity, tone, and topics.
                Never reveal or reuse real message content directly.
                Return only generated cover message.
                """;
    }

    private record GeminiRequest
    {
        public GeminiContent? SystemInstruction { get; init; }
        public List<GeminiContent> Contents { get; init; } = [];
    }

    private record GeminiContent
    {
        public List<GeminiPart> Parts { get; init; } = [];
    }

    private record GeminiPart
    {
        public string? Text { get; init; }
    }

    private record GeminiResponse
    {
        public List<GeminiCandidate>? Candidates { get; init; }
    }

    private record GeminiCandidate
    {
        public GeminiContent? Content { get; init; }
    }
}
