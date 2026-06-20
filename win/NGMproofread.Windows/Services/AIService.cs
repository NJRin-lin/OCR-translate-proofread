using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using NGMproofread.Windows.Models;
using NGMproofread.Windows.Utilities;

namespace NGMproofread.Windows.Services;

public class AIServiceError : Exception
{
    public AIServiceError(string message) : base(message) { }
}

// OpenAI-compatible models
public class ChatMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "";
    [JsonPropertyName("content")]
    public string Content { get; set; } = "";
}

public class ChatRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = "";
    [JsonPropertyName("messages")]
    public List<ChatMessage> Messages { get; set; } = [];
    [JsonPropertyName("temperature")]
    public double Temperature { get; set; } = 0.3;
    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; } = 4096;
}

public class ChatChoice
{
    [JsonPropertyName("message")]
    public ChatMessage Message { get; set; } = new();
    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }
}

public class ChatResponse
{
    [JsonPropertyName("choices")]
    public List<ChatChoice> Choices { get; set; } = [];
}

// Gemini models
public class GeminiPart
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = "";
}

public class GeminiContent
{
    [JsonPropertyName("role")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Role { get; set; }
    [JsonPropertyName("parts")]
    public List<GeminiPart> Parts { get; set; } = [];
}

public class GeminiRequest
{
    [JsonPropertyName("system_instruction")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public GeminiContent? SystemInstruction { get; set; }
    [JsonPropertyName("contents")]
    public List<GeminiContent> Contents { get; set; } = [];
}

public class GeminiCandidate
{
    [JsonPropertyName("content")]
    public GeminiContent Content { get; set; } = new();
}

public class GeminiResponse
{
    [JsonPropertyName("candidates")]
    public List<GeminiCandidate> Candidates { get; set; } = [];
}

public class AIService
{
    private readonly APIKeyStore _store = new();
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(60) };

    public AIProvider ActiveProvider => _store.ActiveProvider;

    public async Task<string> ChatAsync(
        string systemPrompt,
        string userMessage,
        double temperature = 0.3,
        int maxTokens = 4096)
    {
        var provider = _store.ActiveProvider;
        var apiKey = _store.Read(provider);

        if (string.IsNullOrEmpty(apiKey))
            throw new AIServiceError("请先在设置中配置 API Key");

        return provider switch
        {
            AIProvider.DeepSeek or AIProvider.OpenAI =>
                await OpenAIChatAsync(provider, apiKey, systemPrompt, userMessage, temperature, maxTokens),
            AIProvider.Gemini =>
                await GeminiChatAsync(apiKey, systemPrompt, userMessage, maxTokens),
            _ => throw new AIServiceError("未知的 AI 提供商")
        };
    }

    private async Task<string> OpenAIChatAsync(
        AIProvider provider, string apiKey,
        string systemPrompt, string userMessage,
        double temperature, int maxTokens)
    {
        var request = new ChatRequest
        {
            Model = provider.Model(),
            Messages =
            [
                new() { Role = "system", Content = systemPrompt },
                new() { Role = "user", Content = userMessage }
            ],
            Temperature = temperature,
            MaxTokens = maxTokens
        };

        var url = provider.Endpoint();
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
        req.Headers.Add("Authorization", $"Bearer {apiKey}");

        var resp = await _http.SendAsync(req);
        if (!resp.IsSuccessStatusCode)
            throw new AIServiceError($"API 请求失败 (HTTP {(int)resp.StatusCode})");

        var chatResp = await resp.Content.ReadFromJsonAsync<ChatResponse>();
        var msg = chatResp?.Choices?.FirstOrDefault()?.Message?.Content;
        if (string.IsNullOrEmpty(msg))
            throw new AIServiceError("API 返回内容为空");

        return msg;
    }

    private async Task<string> GeminiChatAsync(
        string apiKey, string systemPrompt, string userMessage, int maxTokens)
    {
        var provider = AIProvider.Gemini;
        var url = $"{provider.Endpoint()}?key={apiKey}";

        GeminiContent? sysInstr = null;
        if (!string.IsNullOrEmpty(systemPrompt))
            sysInstr = new GeminiContent { Parts = [new() { Text = systemPrompt }] };

        var request = new GeminiRequest
        {
            SystemInstruction = sysInstr,
            Contents = [new GeminiContent { Role = "user", Parts = [new() { Text = userMessage }] }]
        };

        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var resp = await _http.PostAsync(url, content);
        if (!resp.IsSuccessStatusCode)
            throw new AIServiceError($"API 请求失败 (HTTP {(int)resp.StatusCode})");

        var geminiResp = await resp.Content.ReadFromJsonAsync<GeminiResponse>();
        var msg = geminiResp?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
        if (string.IsNullOrEmpty(msg))
            throw new AIServiceError("API 返回内容为空");

        return msg;
    }
}
