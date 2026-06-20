using NGMproofread.Windows.Models;

namespace NGMproofread.Windows.Services;

public class TranslationService
{
    private readonly AIService _ai = new();

    public async Task<TranslationResult> TranslateAsync(string text, string glossarySnippet = "")
    {
        var systemPrompt = """
            你是一个日文翻译助手。请将以下日文文本翻译成中文。

            要求：
            1. 翻译准确、流畅，符合中文表达习惯
            2. 保留原文的段落结构和格式
            3. 对于专业术语，使用标准的中文译名
            4. 如有歧义，优先选择上下文最合理的译法
            5. 直接返回翻译结果，不要添加任何解释或说明
            """ + glossarySnippet;

        var translated = await _ai.ChatAsync(systemPrompt, text);

        return new TranslationResult
        {
            OriginalText = text,
            TranslatedText = translated.Trim(),
            SourceLanguage = "ja",
            TargetLanguage = "zh-CN"
        };
    }
}
