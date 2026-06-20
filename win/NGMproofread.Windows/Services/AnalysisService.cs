using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using NGMproofread.Windows.Models;

namespace NGMproofread.Windows.Services;

public class AnalysisService
{
    private readonly AIService _ai = new();

    public async Task<AnalysisResult> AnalyzeAsync(string text, AnalysisMode mode)
    {
        var systemPrompt = mode == AnalysisMode.Study ? StudyPrompt : ProofreadPrompt;
        var rawResponse = await _ai.ChatAsync(systemPrompt, text, temperature: 0.3, maxTokens: 4096);
        return ParseResponse(rawResponse, mode);
    }

    private static AnalysisResult ParseResponse(string raw, AnalysisMode mode)
    {
        try
        {
            // Strip markdown code fences
            var json = Regex.Replace(raw, @"```(?:json)?\s*", "");
            json = Regex.Replace(json, @"```\s*$", "");
            json = json.Trim();

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var sentences = new List<SentenceAnalysis>();

            var sentencesArray = root.TryGetProperty("sentences", out var sa) ? sa : root;
            foreach (var sElem in sentencesArray.EnumerateArray())
            {
                var orig = sElem.GetProperty("original").GetString() ?? "";
                var components = ParseComponents(sElem);
                var grammarPoints = sElem.TryGetProperty("grammar_points", out var gp)
                    ? gp.EnumerateArray().Select(g => g.GetString() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToList()
                    : [];
                var vocabulary = sElem.TryGetProperty("vocabulary", out var v)
                    ? ParseVocabulary(v)
                    : [];

                sentences.Add(new SentenceAnalysis
                {
                    OriginalSentence = orig,
                    Components = components,
                    GrammarPoints = grammarPoints,
                    Vocabulary = vocabulary
                });
            }

            string? notes = root.TryGetProperty("overall_notes", out var on) ? on.GetString() : null;

            return new AnalysisResult { Mode = mode, Sentences = sentences, OverallNotes = notes };
        }
        catch (Exception ex)
        {
            return new AnalysisResult
            {
                Mode = mode,
                Sentences = [new SentenceAnalysis { OriginalSentence = raw }],
                OverallNotes = $"解析失败: {ex.Message}"
            };
        }
    }

    private static List<SentenceComponent> ParseComponents(JsonElement parent)
    {
        if (!parent.TryGetProperty("components", out var comps)) return [];

        var result = new List<SentenceComponent>();
        foreach (var c in comps.EnumerateArray())
        {
            result.Add(new SentenceComponent
            {
                Label = c.GetProperty("label").GetString() ?? "",
                Text = c.GetProperty("text").GetString() ?? "",
                Explanation = c.TryGetProperty("explanation", out var e) ? e.GetString() : null,
                Children = ParseComponents(c)
            });
        }
        return result;
    }

    private static List<VocabularyAnnotation> ParseVocabulary(JsonElement vocabElem)
    {
        var result = new List<VocabularyAnnotation>();
        foreach (var v in vocabElem.EnumerateArray())
        {
            result.Add(new VocabularyAnnotation
            {
                Word = v.GetProperty("word").GetString() ?? "",
                Reading = v.GetProperty("reading").GetString() ?? "",
                Meaning = v.GetProperty("meaning").GetString() ?? "",
                PartOfSpeech = v.TryGetProperty("part_of_speech", out var pos) ? pos.GetString() : null,
                JlptLevel = v.TryGetProperty("jlpt_level", out var jl) ? jl.GetString() : null,
                Notes = v.TryGetProperty("notes", out var n) ? n.GetString() : null
            });
        }
        return result;
    }

    private const string ProofreadPrompt = """
        你是一个日语语法分析专家。请分析以下日语句子的语法成分。

        以 JSON 格式返回，结构如下：
        {
          "sentences": [
            {
              "original": "原始日语句子",
              "components": [
                {
                  "label": "主语",
                  "text": "对应的日文文本",
                  "explanation": "简短解释",
                  "children": [
                    {
                      "label": "定语",
                      "text": "修饰语文本",
                      "explanation": "解释"
                    }
                  ]
                }
              ]
            }
          ]
        }

        成分标签使用中文（主语、谓语、宾语、定语、状语、补语等）。
        成分树可以嵌套，children 表示子成分。
        只返回 JSON，不要额外解释。
        """;

    private const string StudyPrompt = """
        你是一个日语教学专家。请对以下日文句子进行深度解析。

        以 JSON 格式返回，结构如下：
        {
          "sentences": [
            {
              "original": "原始日语句子",
              "components": [
                {
                  "label": "主语",
                  "text": "对应的日文文本",
                  "explanation": "简短语法解释",
                  "children": []
                }
              ],
              "grammar_points": ["语法点1：解释+例句", "语法点2：解释+例句"],
              "vocabulary": [
                {
                  "word": "单词（日文）",
                  "reading": "读音（平假名）",
                  "meaning": "中文释义",
                  "part_of_speech": "词性（如：名词、他动词、形容动词等）",
                  "jlpt_level": "N1-N5 或 null",
                  "notes": "用法说明，可空"
                }
              ]
            }
          ],
          "overall_notes": "整体学习建议（可空）"
        }

        要求：
        1. components 进行完整的句子成分拆解，使用中文标签
        2. grammar_points 提取重要语法点，附带解释和例句
        3. vocabulary 重点标注 N1-N2 级别的较难词汇
        4. overall_notes 给出学习建议
        只返回 JSON，不要额外解释。
        """;
}
