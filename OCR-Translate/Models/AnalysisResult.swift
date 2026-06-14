import Foundation

enum AnalysisMode: String, CaseIterable, Equatable {
    case detailed = "详细分析"
    case concise = "简洁分析"
}

struct VocabularyAnnotation: Identifiable, Equatable {
    let id = UUID()
    let word: String
    let reading: String
    let meaning: String
    let partOfSpeech: String?
    let jlptLevel: String?
    let notes: String?
}

struct SentenceComponent: Identifiable, Equatable {
    let id = UUID()
    let label: String
    let text: String
    let explanation: String?
}

struct SentenceAnalysis: Identifiable, Equatable {
    let id = UUID()
    let originalSentence: String
    let components: [SentenceComponent]
    let grammarPoints: [String]
    let vocabulary: [VocabularyAnnotation]
}

struct AnalysisResult: Equatable {
    let mode: AnalysisMode
    let sentences: [SentenceAnalysis]
    let overallNotes: String?
}
