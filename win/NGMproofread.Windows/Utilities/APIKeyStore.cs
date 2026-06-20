using System.IO;
using System.Text.Json;
using NGMproofread.Windows.Models;

namespace NGMproofread.Windows.Utilities;

public class APIKeyStore
{
    private const string ActiveProviderKey = "activeProvider";
    private const string AnalysisModeKey = "defaultAnalysisMode";
    private const string SidebarWidthKey = "layout_sidebarWidth";
    private const string ContentWidthKey = "layout_contentWidth";
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

    public AnalysisMode DefaultAnalysisMode
    {
        get
        {
            if (_cache.TryGetValue(AnalysisModeKey, out var val) &&
                Enum.TryParse<AnalysisMode>(val, out var mode))
                return mode;
            return AnalysisMode.Study;
        }
        set
        {
            _cache[AnalysisModeKey] = value.ToString();
            Save();
        }
    }

    public double SidebarWidth
    {
        get => _cache.TryGetValue(SidebarWidthKey, out var v) && double.TryParse(v, out var d) ? d : 350;
        set { _cache[SidebarWidthKey] = value.ToString("F0"); Save(); }
    }

    public double ContentWidth
    {
        get => _cache.TryGetValue(ContentWidthKey, out var v) && double.TryParse(v, out var d) ? d : 350;
        set { _cache[ContentWidthKey] = value.ToString("F0"); Save(); }
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
        var dir = Path.GetDirectoryName(SettingsPath)!;
        Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(_cache);
        File.WriteAllText(SettingsPath, json);
    }
}
