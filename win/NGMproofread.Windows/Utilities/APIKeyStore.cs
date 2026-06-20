using System.IO;
using System.Text.Json;
using NGMproofread.Windows.Models;

namespace NGMproofread.Windows.Utilities;

public class APIKeyStore
{
    private const string ActiveProviderKey = "activeProvider";
    private const string DeepSeekKey = "apiKey_deepseek";
    private const string OpenAIKey = "apiKey_openai";
    private const string GeminiKey = "apiKey_gemini";

    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NGMproofread", "settings.json");

    private Dictionary<string, string> _cache = new();

    public APIKeyStore()
    {
        Load();
    }

    public AIProvider ActiveProvider
    {
        get
        {
            if (_cache.TryGetValue(ActiveProviderKey, out var val) &&
                Enum.TryParse<AIProvider>(val, out var provider))
                return provider;
            return AIProvider.DeepSeek;
        }
        set
        {
            _cache[ActiveProviderKey] = value.ToString();
            Save();
        }
    }

    public string? Read(AIProvider provider)
    {
        var key = provider switch
        {
            AIProvider.DeepSeek => DeepSeekKey,
            AIProvider.OpenAI => OpenAIKey,
            AIProvider.Gemini => GeminiKey,
            _ => ""
        };
        return _cache.TryGetValue(key, out var val) ? val : null;
    }

    public void Save(string apiKey, AIProvider provider)
    {
        var key = provider switch
        {
            AIProvider.DeepSeek => DeepSeekKey,
            AIProvider.OpenAI => OpenAIKey,
            AIProvider.Gemini => GeminiKey,
            _ => ""
        };
        _cache[key] = apiKey;
        Save();
    }

    public void Delete(AIProvider provider)
    {
        var key = provider switch
        {
            AIProvider.DeepSeek => DeepSeekKey,
            AIProvider.OpenAI => OpenAIKey,
            AIProvider.Gemini => GeminiKey,
            _ => ""
        };
        _cache.Remove(key);
        Save();
    }

    public bool HasActiveKey()
    {
        return !string.IsNullOrEmpty(Read(ActiveProvider));
    }

    private void Load()
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingsPath)!;
            Directory.CreateDirectory(dir);
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                _cache = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
            }
        }
        catch { _cache = new(); }
    }

    private void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingsPath)!;
            Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(_cache);
            File.WriteAllText(SettingsPath, json);
        }
        catch { }
    }
}
