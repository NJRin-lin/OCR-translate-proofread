import Foundation

enum AnalysisError: LocalizedError {
    case emptyText
    case apiError(String)
    case parseError(String)

    var errorDescription: String? {
        switch self {
        case .emptyText: "没有可分析的文本"
        case .apiError(let msg): "分析出错: \(msg)"
        case .parseError(let msg): "解析分析结果出错: \(msg)"
        }
    }
}

final class AnalysisService {
    private let deepSeek = DeepSeekService()

    // MARK: - Prompts

    private func systemPrompt(for mode: AnalysisMode) -> String {
        switch mode {
        case .detailed:
            return """
            你是一位专业的日语教师，擅长分析日语句子的语法结构和词汇。

            请对以下日文句子进行详细分析，以 JSON 格式返回结果：

            {
              "sentences": [
                {
                  "original": "原句",
                  "components": [
                    {"label": "主语", "text": "...", "explanation": "简要说明"},
                    {"label": "谓语", "text": "...", "explanation": "简要说明"},
                    {"label": "宾语", "text": "...", "explanation": "简要说明"},
                    {"label": "定语", "text": "...", "explanation": "简要说明"},
                    {"label": "状语", "text": "...", "explanation": "简要说明"}
                  ],
                  "grammarPoints": ["语法点1：说明", "语法点2：说明"],
                  "vocabulary": [
                    {"word": "単語", "reading": "たんご", "meaning": "单词", "partOfSpeech": "名词", "jlptLevel": "N3", "notes": "补充说明"}
                  ]
                }
              ],
              "overallNotes": "整体分析备注"
            }

            要求：
            - 拆分每个句子的主谓宾定状补成分，如果某个成分不存在则省略
            - 标注所有 N2 及以上级别的词汇，或较罕见的表达
            - 语法点要简洁但准确
            - 词汇注音使用平假名
            - 只输出 JSON，不要有其他内容
            """
        case .concise:
            return """
            你是一位专业的日语教师。请对以下日文句子进行简洁分析，以 JSON 格式返回结果：

            {
              "sentences": [
                {
                  "original": "原句",
                  "grammarPoints": ["关键语法点1", "关键语法点2"],
                  "vocabulary": [
                    {"word": "単語", "reading": "たんご", "meaning": "单词", "partOfSpeech": "名词", "jlptLevel": "N1"}
                  ]
                }
              ],
              "overallNotes": "一句话总结"
            }

            要求：
            - 只标注最关键或最不常见的语法结构
            - 只标注 N1 级别或非常罕见的词汇
            - 词汇注音使用平假名
            - 只输出 JSON，不要有其他内容
            """
        }
    }

    // MARK: - Parse

    func analyze(text: String, mode: AnalysisMode) async throws -> AnalysisResult {
        let trimmed = text.trimmingCharacters(in: .whitespacesAndNewlines)
        guard !trimmed.isEmpty else {
            throw AnalysisError.emptyText
        }

        let prompt = systemPrompt(for: mode)
        let rawResponse: String
        do {
            rawResponse = try await deepSeek.chat(
                systemPrompt: prompt,
                userMessage: trimmed,
                temperature: 0.2,
                maxTokens: 4096
            )
        } catch {
            throw AnalysisError.apiError(error.localizedDescription)
        }

        return try parse(rawResponse, mode: mode)
    }

    private func parse(_ jsonString: String, mode: AnalysisMode) throws -> AnalysisResult {
        let cleaned = jsonString
            .replacingOccurrences(of: "```json", with: "")
            .replacingOccurrences(of: "```", with: "")
            .trimmingCharacters(in: .whitespacesAndNewlines)

        guard let data = cleaned.data(using: .utf8) else {
            throw AnalysisError.parseError("无法编码响应文本")
        }

        guard let json = try? JSONSerialization.jsonObject(with: data) as? [String: Any] else {
            throw AnalysisError.parseError("JSON 解析失败")
        }

        let sentencesJSON = json["sentences"] as? [[String: Any]] ?? []
        let sentences: [SentenceAnalysis] = sentencesJSON.compactMap { sentenceJSON in
            let original = sentenceJSON["original"] as? String ?? ""

            let componentsJSON = sentenceJSON["components"] as? [[String: Any]] ?? []
            let components = componentsJSON.compactMap { comp -> SentenceComponent? in
                guard let label = comp["label"] as? String,
                      let text = comp["text"] as? String else { return nil }
                return SentenceComponent(
                    label: label,
                    text: text,
                    explanation: comp["explanation"] as? String
                )
            }

            let grammarPoints = sentenceJSON["grammarPoints"] as? [String] ?? []

            let vocabJSON = sentenceJSON["vocabulary"] as? [[String: Any]] ?? []
            let vocabulary = vocabJSON.compactMap { vocab -> VocabularyAnnotation? in
                guard let word = vocab["word"] as? String,
                      let reading = vocab["reading"] as? String,
                      let meaning = vocab["meaning"] as? String else { return nil }
                return VocabularyAnnotation(
                    word: word,
                    reading: reading,
                    meaning: meaning,
                    partOfSpeech: vocab["partOfSpeech"] as? String,
                    jlptLevel: vocab["jlptLevel"] as? String,
                    notes: vocab["notes"] as? String
                )
            }

            return SentenceAnalysis(
                originalSentence: original,
                components: components,
                grammarPoints: grammarPoints,
                vocabulary: vocabulary
            )
        }

        return AnalysisResult(
            mode: mode,
            sentences: sentences,
            overallNotes: json["overallNotes"] as? String
        )
    }
}
