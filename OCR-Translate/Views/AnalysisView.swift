import SwiftUI

struct AnalysisView: View {
    let result: AnalysisResult?

    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            HStack {
                Text("句子分析")
                    .font(.headline)
                if let mode = result?.mode {
                    Spacer()
                    Text(mode.rawValue)
                        .font(.caption)
                        .foregroundStyle(.secondary)
                        .padding(.horizontal, 8)
                        .padding(.vertical, 3)
                        .background(Capsule().fill(.quaternary))
                }
            }

            if let result = result, !result.sentences.isEmpty {
                ForEach(result.sentences) { sentence in
                    sentenceBox(sentence)
                }

                if let notes = result.overallNotes, !notes.isEmpty {
                    overallNotesBox(notes)
                }
            } else if result != nil {
                Text("无分析结果")
                    .foregroundStyle(.secondary)
            }
        }
    }

    // MARK: - Sentence Box

    private func sentenceBox(_ sentence: SentenceAnalysis) -> some View {
        VStack(alignment: .leading, spacing: 0) {
            // Original sentence header
            Text(sentence.originalSentence)
                .font(.system(.body, design: .serif).bold())
                .textSelection(.enabled)
                .padding(.horizontal, 14)
                .padding(.vertical, 10)

            if !sentence.components.isEmpty {
                Divider()
                VStack(alignment: .leading, spacing: 2) {
                    Text("句子成分")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                        .padding(.horizontal, 14)
                        .padding(.top, 8)
                        .padding(.bottom, 6)

                    componentTable(sentence.components)
                        .padding(.horizontal, 14)
                }
            }

            if !sentence.grammarPoints.isEmpty {
                Divider()
                grammarSection(sentence.grammarPoints)
            }

            if !sentence.vocabulary.isEmpty {
                let filtered = filterVocab(sentence.vocabulary)
                if !filtered.isEmpty {
                    Divider()
                    vocabularySection(filtered)
                }
            }
        }
        .background(
            RoundedRectangle(cornerRadius: 8)
                .fill(.background)
        )
        .overlay(
            RoundedRectangle(cornerRadius: 8)
                .stroke(.quaternary, lineWidth: 0.5)
        )
    }

    // MARK: - Component Table

    private func componentTable(_ components: [SentenceComponent]) -> some View {
        let lines = buildTableLines(components)
        return Text(verbatim: lines.joined(separator: "\n"))
            .font(.system(.caption, design: .monospaced))
            .foregroundStyle(.primary)
            .padding(10)
            .frame(maxWidth: .infinity, alignment: .leading)
            .background(
                RoundedRectangle(cornerRadius: 4)
                    .fill(.quaternary.opacity(0.5))
            )
    }

    private func buildTableLines(_ components: [SentenceComponent]) -> [String] {
        // Column widths
        let labelColW = max(6, components.map { $0.label.count }.max() ?? 0) + 2
        let textColW = max(6, components.map { $0.text.count }.max() ?? 0) + 2
        let explColW = max(6, components.map { $0.explanation?.count ?? 0 }.max() ?? 0) + 2

        func pad(_ s: String, _ w: Int) -> String {
            let diff = w - s.count
            guard diff > 0 else { return s }
            return s + String(repeating: " ", count: diff)
        }

        let sep = "├" + String(repeating: "─", count: labelColW + 1) + "┼"
                     + String(repeating: "─", count: textColW + 1) + "┼"
                     + String(repeating: "─", count: explColW + 1) + "┤"

        var lines: [String] = []

        // Header
        let header = "│ " + pad("成分", labelColW) + "│ " + pad("原文", textColW) + "│ " + pad("说明", explColW) + "│"

        // Top border
        let top = "┌" + String(repeating: "─", count: labelColW + 1) + "┬"
                   + String(repeating: "─", count: textColW + 1) + "┬"
                   + String(repeating: "─", count: explColW + 1) + "┐"

        lines.append(top)
        lines.append(header)
        lines.append(sep)

        for comp in components {
            let label = pad(comp.label, labelColW)
            let text = pad(comp.text, textColW)
            let expl = pad(comp.explanation ?? "", explColW)
            lines.append("│ " + label + "│ " + text + "│ " + expl + "│")
        }

        // Bottom border
        let bot = "└" + String(repeating: "─", count: labelColW + 1) + "┴"
                   + String(repeating: "─", count: textColW + 1) + "┴"
                   + String(repeating: "─", count: explColW + 1) + "┘"
        lines.append(bot)

        return lines
    }

    // MARK: - Helpers

    private func filterVocab(_ words: [VocabularyAnnotation]) -> [VocabularyAnnotation] {
        guard result?.mode == .study else { return words }
        return words.filter { vocab in
            guard let level = vocab.jlptLevel else { return true }
            let n = Int(level.dropFirst(1)) ?? 0
            return n <= 2
        }
    }

    // MARK: - Grammar Section

    private func grammarSection(_ points: [String]) -> some View {
        VStack(alignment: .leading, spacing: 4) {
            Text("语法要点")
                .font(.caption)
                .foregroundStyle(.secondary)
                .padding(.horizontal, 14)
                .padding(.top, 8)
                .padding(.bottom, 4)

            ForEach(points, id: \.self) { point in
                HStack(alignment: .top, spacing: 6) {
                    Text("·")
                        .font(.body)
                        .foregroundStyle(.purple)
                    Text(point)
                        .font(.callout)
                }
                .padding(.horizontal, 14)
            }
            .padding(.bottom, 2)
        }
    }

    // MARK: - Vocabulary Section

    private func vocabularySection(_ words: [VocabularyAnnotation]) -> some View {
        VStack(alignment: .leading, spacing: 4) {
            Text("词汇注解")
                .font(.caption)
                .foregroundStyle(.secondary)
                .padding(.horizontal, 14)
                .padding(.top, 8)
                .padding(.bottom, 4)

            ForEach(words) { word in
                VStack(alignment: .leading, spacing: 2) {
                    HStack(spacing: 6) {
                        Text(word.word)
                            .font(.system(.callout, design: .serif).bold())

                        Text("(\(word.reading))")
                            .font(.caption)
                            .foregroundStyle(.secondary)

                        Text(word.meaning)
                            .font(.callout)

                        if let pos = word.partOfSpeech {
                            Text(pos)
                                .font(.caption2)
                                .foregroundStyle(.blue)
                                .padding(.horizontal, 5)
                                .padding(.vertical, 1)
                                .background(Capsule().fill(.blue.opacity(0.1)))
                        }

                        if let level = word.jlptLevel {
                            Text(level)
                                .font(.caption2)
                                .foregroundStyle(.orange)
                                .padding(.horizontal, 5)
                                .padding(.vertical, 1)
                                .background(Capsule().fill(.orange.opacity(0.1)))
                        }
                    }

                    if let notes = word.notes {
                        Text(notes)
                            .font(.caption)
                            .foregroundStyle(.secondary)
                    }
                }
                .padding(.horizontal, 14)
                .padding(.bottom, 4)
            }
        }
    }

    // MARK: - Overall Notes

    private func overallNotesBox(_ notes: String) -> some View {
        HStack(alignment: .top, spacing: 6) {
            Image(systemName: "lightbulb.fill")
                .font(.caption)
                .foregroundStyle(.yellow)
            Text(notes)
                .font(.callout)
                .foregroundStyle(.secondary)
        }
        .padding(12)
        .background(
            RoundedRectangle(cornerRadius: 8)
                .fill(.yellow.opacity(0.06))
        )
        .overlay(
            RoundedRectangle(cornerRadius: 8)
                .stroke(.yellow.opacity(0.2), lineWidth: 0.5)
        )
    }
}
