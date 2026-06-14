import Foundation

struct TranslationResult: Equatable {
    let originalText: String
    let translatedText: String
    let sourceLanguage: String
    let targetLanguage: String
    let timestamp: Date

    init(
        originalText: String,
        translatedText: String,
        sourceLanguage: String = "ja",
        targetLanguage: String = "zh-CN",
        timestamp: Date = Date()
    ) {
        self.originalText = originalText
        self.translatedText = translatedText
        self.sourceLanguage = sourceLanguage
        self.targetLanguage = targetLanguage
        self.timestamp = timestamp
    }
}
