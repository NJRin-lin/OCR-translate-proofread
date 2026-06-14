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
        case .proofread:
            return """
            你是一位专业的日语校对专家。用户会给你一段日文文本（OCR 识别结果，没有换行和空格）。
            请先按句号「。」感叹号「！」问号「？」断句，然后对每个句子精准拆解语法成分。
            你需要精确标注每个成分对应的原文片段，帮助用户核对翻译是否准确。

            {
              "sentences": [
                {
                  "original": "完整的原句",
                  "components": [
                    {"label": "主语", "text": "原文片段", "explanation": "指代/施事者说明"},
                    {"label": "谓语", "text": "原文片段", "explanation": "时态/语态/敬体说明"},
                    {"label": "宾语", "text": "原文片段", "explanation": "受事对象"},
                    {"label": "定语", "text": "原文片段", "explanation": "修饰对象"},
                    {"label": "状语", "text": "原文片段", "explanation": "时间/地点/方式"},
                    {"label": "补语", "text": "原文片段", "explanation": "补充说明"}
                  ]
                }
              ]
            }

            要求：
            - 按句号「。」感叹号「！」问号「？」断句，逐句分析
            - 每一句必须包含主语和谓语，其他成分存在则标注，不存在则省略
            - explanation 用简洁中文说明该成分在句中的作用
            - 不需要语法点和词汇注解
            - 只输出 JSON，不要有任何其他文字
            """
        case .study:
            return """
            你是一位专业的日语教师，擅长教学分析。用户会给你一段日文文本（OCR 识别结果，没有换行和空格）。
            请按以下顺序分析：

            1. 按句号「。」感叹号「！」问号「？」断句
            2. 对每句拆解语法成分（主语·谓语·宾语·定语·状语·补语），标注原文片段和说明
            3. 列出关键语法点，附简要解释和 1 个例句
            4. 标注所有 JLPT N2~N1 级别词汇，以及值得学习的常用表达

            {
              "sentences": [
                {
                  "original": "完整的原句",
                  "components": [
                    {"label": "主语", "text": "原文片段", "explanation": "简要说明"},
                    {"label": "谓语", "text": "原文片段", "explanation": "简要说明"}
                  ],
                  "grammarPoints": ["语法格式：简要解释。例句：简短例句。"],
                  "vocabulary": [
                    {"word": "単語", "reading": "たんご", "meaning": "单词", "partOfSpeech": "名词", "jlptLevel": "N2", "notes": "常见用法/易错提示"}
                  ]
                }
              ],
              "overallNotes": "整体学习建议"
            }

            要求：
            - 语法点必须附 1 个简短例句，解释使用场景
            - 词汇注音用平假名，释义用中文，notes 可包含常见搭配或易错点
            - 不存在的成分省略
            - 只输出 JSON
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
