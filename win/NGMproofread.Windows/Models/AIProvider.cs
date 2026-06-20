namespace NGMproofread.Windows.Models;

public enum AIProvider
{
    DeepSeek,
    OpenAI,
    Gemini
}

public static class AIProviderExtensions
{
    public static string DisplayName(this AIProvider provider) => provider switch
    {
        AIProvider.DeepSeek => "DeepSeek",
        AIProvider.OpenAI => "ChatGPT (OpenAI)",
        AIProvider.Gemini => "Gemini (Google)",
        _ => ""
    };

    public static string ShortName(this AIProvider provider) => provider switch
    {
        AIProvider.DeepSeek => "DeepSeek",
        AIProvider.OpenAI => "OpenAI",
        AIProvider.Gemini => "Gemini",
        _ => ""
    };

    public static string Endpoint(this AIProvider provider) => provider switch
    {
        AIProvider.DeepSeek => "https://api.deepseek.com/v1/chat/completions",
        AIProvider.OpenAI => "https://api.openai.com/v1/chat/completions",
        AIProvider.Gemini => "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent",
        _ => ""
    };

    public static string Model(this AIProvider provider) => provider switch
    {
        AIProvider.DeepSeek => "deepseek-chat",
        AIProvider.OpenAI => "gpt-4o",
        AIProvider.Gemini => "gemini-2.0-flash",
        _ => ""
    };

    public static string KeyPrefix(this AIProvider provider) => provider switch
    {
        AIProvider.DeepSeek => "sk-",
        AIProvider.OpenAI => "sk-",
        AIProvider.Gemini => "AIza",
        _ => ""
    };

    public static string GetKeyURL(this AIProvider provider) => provider switch
    {
        AIProvider.DeepSeek => "platform.deepseek.com -> API Keys",
        AIProvider.OpenAI => "platform.openai.com -> API Keys",
        AIProvider.Gemini => "aistudio.google.com -> Get API Key",
        _ => ""
    };
}
