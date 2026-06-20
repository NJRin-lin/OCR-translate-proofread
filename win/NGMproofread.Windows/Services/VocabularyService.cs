using System.Text.Json;
using NGMproofread.Windows.Models;

namespace NGMproofread.Windows.Services;

public class VocabEntry
{
    public string Word { get; init; } = "";
    public string Reading { get; init; } = "";
    public string Meaning { get; init; } = "";
    public string? PartOfSpeech { get; init; }
    public string? JlptLevel { get; init; }
    public List<string> Examples { get; init; } = [];
    public string? Notes { get; init; }
}

public class VocabularyService
{
    private readonly AIService _ai = new();
    private readonly Dictionary<string, VocabEntry> _cache = new();

    public async Task<VocabEntry> LookupAsync(string word)
    {
        if (_cache.TryGetValue(word, out var cached))
            return cached;

        var systemPrompt = """
            你是一个日语词典。请对用户查询的日语单词或短语给出详细解释。

            以 JSON 格式返回：
            {
              "word": "原始单词",
              "reading": "平假名读音",
              "meaning": "中文释义",
              "part_of_speech": "词性（如：名词、他动词、形容动词、副词、助词等）",
              "jlpt_level": "JLPT等级（N1-N5，不确定填 null）",
              "examples": ["例句1", "例句2"],
              "notes": "用法说明或补充信息（如无则填 null）"
            }

            只返回 JSON，不要额外解释。
            """;

        var raw = await _ai.ChatAsync(systemPrompt, word, temperature: 0.3, maxTokens: 2048);

        try
        {
            var json = raw.Trim();
            if (json.StartsWith("```")) json = json[json.IndexOf('\n')..].Trim();
            if (json.EndsWith("```")) json = json[..json.LastIndexOf("```")].Trim();

            var entry = JsonSerializer.Deserialize<VocabEntry>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new VocabEntry { Word = word, Reading = word, Meaning = raw };

            _cache[word] = entry;
            return entry;
        }
        catch
        {
            var fallback = new VocabEntry
            {
                Word = word,
                Reading = word,
                Meaning = raw.Trim(),
                Notes = null
            };
            _cache[word] = fallback;
            return fallback;
        }
    }
}
