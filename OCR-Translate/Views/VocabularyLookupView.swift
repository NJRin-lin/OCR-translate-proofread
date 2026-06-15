import SwiftUI

struct VocabularyLookupView: View {
    @Binding var externalQuery: String
    @State private var query: String = ""
    @State private var result: VocabEntry?
    @State private var isLoading = false
    @State private var errorMessage: String?
    @State private var isExpanded = true
    @State private var notesExpanded = false

    private let service = VocabularyService()
    @FocusState private var isFocused: Bool

    var body: some View {
        VStack(spacing: 0) {
            HStack(spacing: 6) {
                Image(systemName: "character.magnify")
                    .foregroundStyle(.secondary)
                    .font(.caption)

                TextField("查词...", text: $query)
                    .textFieldStyle(.roundedBorder)
                    .font(.caption)
                    .frame(minWidth: 100)
                    .focused($isFocused)
                    .onSubmit { performLookup() }

                if !query.isEmpty {
                    Button(action: {
                        query = ""
                        isExpanded = false
                    }) {
                        Image(systemName: "xmark.circle.fill")
                            .foregroundStyle(.secondary)
                            .font(.caption)
                    }
                    .buttonStyle(.borderless)
                }

                Spacer()

                Button(action: performLookup) {
                    if isLoading {
                        ProgressView()
                            .scaleEffect(0.6)
                    } else {
                        Label("查询", systemImage: "magnifyingglass")
                    }
                }
                .buttonStyle(.borderedProminent)
                .controlSize(.small)
                .disabled(query.trimmingCharacters(in: .whitespaces).isEmpty || isLoading)

                if let _ = result {
                    Button(action: { isExpanded.toggle() }) {
                        Label(isExpanded ? "收起" : "展开",
                              systemImage: isExpanded ? "chevron.up" : "chevron.down")
                    }
                    .buttonStyle(.borderedProminent)
                    .controlSize(.small)
                }
            }
            .padding(.horizontal, 14)
            .padding(.vertical, 10)
            .background(.bar)

            if let error = errorMessage {
                Text(error)
                    .font(.caption)
                    .foregroundStyle(.red)
                    .padding(.horizontal, 12)
                    .padding(.top, 2)
            }
        }
        .overlay(alignment: .top) {
            if let entry = result, isExpanded {
                VStack {
                    Spacer().frame(height: 40)
                    vocabCard(entry)
                        .shadow(color: .black.opacity(0.15), radius: 10, y: 5)
                        .padding(.horizontal, 4)
                }
            }
        }
        .zIndex(2)
        .onChange(of: externalQuery) { _, newValue in
            guard !newValue.isEmpty else { return }
            query = newValue
            externalQuery = ""
            performLookup()
        }
        .onAppear {
            NSEvent.addLocalMonitorForEvents(matching: .keyDown) { event in
                if event.modifierFlags.contains(.command), event.charactersIgnoringModifiers == "k" {
                    DispatchQueue.main.async { isFocused = true }
                    return nil
                }
                return event
            }
        }
    }

    // MARK: - Vocab Card

    private func vocabCard(_ entry: VocabEntry) -> some View {
        VStack(alignment: .leading, spacing: 6) {
            HStack(spacing: 6) {
                Text(entry.word)
                    .font(.system(.body, design: .serif).bold())
                    .textSelection(.enabled)
                Text("(\(entry.reading))")
                    .font(.caption)
                    .foregroundStyle(.secondary)
                    .textSelection(.enabled)

                if let pos = entry.partOfSpeech {
                    Text(pos)
                        .font(.caption2)
                        .foregroundStyle(.blue)
                        .padding(.horizontal, 4)
                        .padding(.vertical, 1)
                        .background(Capsule().fill(.blue.opacity(0.1)))
                }
                if let level = entry.jlptLevel {
                    Text(level)
                        .font(.caption2)
                        .foregroundStyle(.orange)
                        .padding(.horizontal, 4)
                        .padding(.vertical, 1)
                        .background(Capsule().fill(.orange.opacity(0.1)))
                }
            }

            Text(entry.meaning)
                .font(.callout)
                .textSelection(.enabled)

            if let notes = entry.notes {
                VStack(alignment: .leading, spacing: 2) {
                    Text(notes)
                        .font(.caption)
                        .foregroundStyle(.secondary)
                        .textSelection(.enabled)
                        .lineLimit(notesExpanded ? nil : 2)

                    if notes.count >= 60 {
                        Button(action: { notesExpanded.toggle() }) {
                            Text(notesExpanded ? "收起" : "展开全部")
                                .font(.caption)
                                .foregroundStyle(.blue)
                        }
                        .buttonStyle(.borderless)
                    }
                }
            }

            if !entry.examples.isEmpty {
                VStack(alignment: .leading, spacing: 3) {
                    ForEach(entry.examples, id: \.self) { ex in
                        HStack(alignment: .top, spacing: 4) {
                            Text("·").foregroundStyle(.purple).font(.caption)
                            Text(ex).font(.caption).textSelection(.enabled)
                        }
                    }
                }
                .padding(.top, 2)
            }
        }
        .padding(10)
        .background(
            RoundedRectangle(cornerRadius: 6)
                .fill(.background)
        )
        .overlay(
            RoundedRectangle(cornerRadius: 6)
                .stroke(.quaternary, lineWidth: 0.5)
        )
        .padding(.horizontal, 12)
    }

    func performLookup() {
        let trimmed = query.trimmingCharacters(in: .whitespaces)
        guard !trimmed.isEmpty else { return }

        isLoading = true
        errorMessage = nil
        result = nil
        isExpanded = true
        notesExpanded = false

        Task {
            do {
                let entry = try await service.lookup(trimmed)
                result = entry
            } catch {
                errorMessage = error.localizedDescription
            }
            isLoading = false
        }
    }
}
