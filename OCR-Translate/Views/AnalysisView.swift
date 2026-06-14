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
                    sentenceAnalysisView(sentence)
                }

                if let notes = result.overallNotes, !notes.isEmpty {
                    Divider()
                    HStack(alignment: .top, spacing: 4) {
                        Image(systemName: "lightbulb.fill")
                            .font(.caption)
                            .foregroundStyle(.yellow)
                        Text(notes)
                            .font(.callout)
                            .foregroundStyle(.secondary)
                    }
                }
            } else if result != nil {
                Text("无分析结果")
                    .foregroundStyle(.secondary)
            }
        }
    }

    // MARK: - Sentence View

    private func sentenceAnalysisView(_ sentence: SentenceAnalysis) -> some View {
        VStack(alignment: .leading, spacing: 10) {
            Text(sentence.originalSentence)
                .font(.body.bold())
                .padding(.horizontal, 8)
                .padding(.vertical, 6)
                .background(
                    RoundedRectangle(cornerRadius: 6)
                        .fill(.quaternary)
                )

            if !sentence.components.isEmpty {
                VStack(alignment: .leading, spacing: 4) {
                    Text("句子成分")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                    ForEach(sentence.components) { comp in
                        HStack(alignment: .firstTextBaseline) {
                            Text("[\(comp.label)]")
                                .font(.caption)
                                .foregroundStyle(.blue)
                                .frame(width: 48, alignment: .leading)
                            Text(comp.text)
                                .font(.callout)
                            if let explanation = comp.explanation {
                                Text(explanation)
                                    .font(.caption)
                                    .foregroundStyle(.secondary)
                            }
                        }
                    }
                }
            }

            if !sentence.grammarPoints.isEmpty {
                VStack(alignment: .leading, spacing: 4) {
                    Text("语法要点")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                    ForEach(sentence.grammarPoints, id: \.self) { point in
                        HStack(alignment: .top, spacing: 4) {
                            Image(systemName: "circle.fill")
                                .font(.system(size: 4))
                                .padding(.top, 6)
                            Text(point)
                                .font(.callout)
                        }
                    }
                }
            }

            if !sentence.vocabulary.isEmpty {
                vocabularyView(sentence.vocabulary)
            }
        }
        .padding()
        .background(
            RoundedRectangle(cornerRadius: 8)
                .fill(.background)
                .overlay(
                    RoundedRectangle(cornerRadius: 8)
                        .stroke(.quaternary, lineWidth: 0.5)
                )
        )
    }

    // MARK: - Vocabulary View

    private func vocabularyView(_ words: [VocabularyAnnotation]) -> some View {
        VStack(alignment: .leading, spacing: 6) {
            Text("词汇注解")
                .font(.caption)
                .foregroundStyle(.secondary)

            ForEach(words) { word in
                HStack(alignment: .firstTextBaseline) {
                    Text(word.word)
                        .font(.callout.bold())
                    Text("(\(word.reading))")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                    Text(word.meaning)
                        .font(.callout)

                    if let pos = word.partOfSpeech {
                        Text(pos)
                            .font(.caption2)
                            .foregroundStyle(.blue)
                            .padding(.horizontal, 4)
                            .padding(.vertical, 1)
                            .background(Capsule().fill(.blue.opacity(0.1)))
                    }

                    if let level = word.jlptLevel {
                        Text(level)
                            .font(.caption2)
                            .foregroundStyle(.orange)
                            .padding(.horizontal, 4)
                            .padding(.vertical, 1)
                            .background(Capsule().fill(.orange.opacity(0.1)))
                    }

                    Spacer()
                }

                if let notes = word.notes {
                    Text(notes)
                        .font(.caption)
                        .foregroundStyle(.secondary)
                        .padding(.leading, 4)
                }
            }
        }
    }
}
