namespace NGMproofread.Windows.Models;

public class TranslationResult
{
    public string OriginalText { get; init; } = "";
    public string TranslatedText { get; init; } = "";
    public string SourceLanguage { get; init; } = "ja";
    public string TargetLanguage { get; init; } = "zh-CN";
    public DateTime Timestamp { get; init; } = DateTime.Now;
}
