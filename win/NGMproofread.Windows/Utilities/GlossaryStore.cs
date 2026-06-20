using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace NGMproofread.Windows.Utilities;

public class GlossaryEntry
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Word { get; init; } = "";
    public string Translation { get; init; } = "";
}

public class GlossaryStore : INotifyPropertyChanged
{
    private const string GlossaryKey = "glossary";

    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NGMproofread", "glossary.json");

    private List<GlossaryEntry> _entries = [];

    public event PropertyChangedEventHandler? PropertyChanged;

    public ReadOnlyCollection<GlossaryEntry> Entries => _entries.AsReadOnly();

    public GlossaryStore()
    {
        Load();
    }

    public void Add(string word, string translation)
    {
        var w = word.Trim();
        var t = translation.Trim();
        if (string.IsNullOrEmpty(w) || string.IsNullOrEmpty(t)) return;
        if (_entries.Count >= 50) return;

        _entries.Add(new GlossaryEntry { Word = w, Translation = t });
        Save();
        OnPropertyChanged(nameof(Entries));
    }

    public void Remove(GlossaryEntry entry)
    {
        _entries.RemoveAll(e => e.Id == entry.Id);
        Save();
        OnPropertyChanged(nameof(Entries));
    }

    public string BuildPromptSnippet()
    {
        if (_entries.Count == 0) return "";

        var items = _entries.Select(e => $"- 「{e.Word}」→「{e.Translation}」");
        return $"""

            术语表（翻译时请严格按照以下映射）：
            {string.Join("\n", items)}
            """;
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
                _entries = JsonSerializer.Deserialize<List<GlossaryEntry>>(json) ?? [];
            }
        }
        catch { _entries = []; }
    }

    private void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingsPath)!;
            Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(_entries);
            File.WriteAllText(SettingsPath, json);
        }
        catch { }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
