namespace NGMproofread.Windows.Models;

public enum AnalysisMode
{
    Proofread,
    Study
}

public static class AnalysisModeExtensions
{
    public static string DisplayName(this AnalysisMode mode) => mode switch
    {
        AnalysisMode.Proofread => "校对模式",
        AnalysisMode.Study => "学习模式",
        _ => ""
    };
}

public class VocabularyAnnotation
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Word { get; init; } = "";
    public string Reading { get; init; } = "";
    public string Meaning { get; init; } = "";
    public string? PartOfSpeech { get; init; }
    public string? JlptLevel { get; init; }
    public string? Notes { get; init; }
}

public class SentenceComponent
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Label { get; init; } = "";
    public string Text { get; init; } = "";
    public string? Explanation { get; init; }
    public List<SentenceComponent> Children { get; init; } = [];
}

public class SentenceAnalysis
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string OriginalSentence { get; init; } = "";
    public List<SentenceComponent> Components { get; init; } = [];
    public List<string> GrammarPoints { get; init; } = [];
    public List<VocabularyAnnotation> Vocabulary { get; init; } = [];
}

public class AnalysisResult
{
    public AnalysisMode Mode { get; init; }
    public List<SentenceAnalysis> Sentences { get; init; } = [];
    public string? OverallNotes { get; init; }
}
