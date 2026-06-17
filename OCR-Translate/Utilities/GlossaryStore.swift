import Foundation

struct GlossaryEntry: Codable, Identifiable, Equatable {
    var id = UUID()
    var word: String
    var translation: String
}

final class GlossaryStore: ObservableObject {
    @Published var entries: [GlossaryEntry] = []

    private let key = "custom_glossary"
    private let defaults = UserDefaults.standard

    init() { load() }

    func add(word: String, translation: String) {
        let trimmedWord = word.trimmingCharacters(in: .whitespaces)
        let trimmedTrans = translation.trimmingCharacters(in: .whitespaces)
        guard !trimmedWord.isEmpty, !trimmedTrans.isEmpty else { return }
        entries.append(GlossaryEntry(word: trimmedWord, translation: trimmedTrans))
        save()
    }

    func remove(_ entry: GlossaryEntry) {
        entries.removeAll { $0.id == entry.id }
        save()
    }

    func buildPromptSnippet() -> String {
        guard !entries.isEmpty else { return "" }
        let lines = entries.map { "「\($0.word)」→「\($0.translation)」" }
        return "\n\n术语表（请严格按以下映射翻译这些词汇）：\n" + lines.joined(separator: "\n")
    }

    private func save() {
        guard let data = try? JSONEncoder().encode(entries) else { return }
        defaults.set(data, forKey: key)
    }

    private func load() {
        guard let data = defaults.data(forKey: key),
              let decoded = try? JSONDecoder().decode([GlossaryEntry].self, from: data)
        else { return }
        entries = decoded
    }
}
