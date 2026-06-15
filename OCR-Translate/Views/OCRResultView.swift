import SwiftUI
import AVFoundation

// MARK: - Speech controller

private final class SpeechController: NSObject, ObservableObject, AVSpeechSynthesizerDelegate {
    @Published var isSpeaking = false
    private let synth = AVSpeechSynthesizer()

    override init() {
        super.init()
        synth.delegate = self
    }

    func speak(_ text: String, language: String) {
        synth.stopSpeaking(at: .immediate)
        let utterance = AVSpeechUtterance(string: text)
        utterance.voice = AVSpeechSynthesisVoice(language: language)
        utterance.rate = AVSpeechUtteranceDefaultSpeechRate
        synth.speak(utterance)
        isSpeaking = true
    }

    func stop() {
        synth.stopSpeaking(at: .immediate)
        isSpeaking = false
    }

    func speechSynthesizer(_ synthesizer: AVSpeechSynthesizer, didFinish utterance: AVSpeechUtterance) {
        isSpeaking = false
    }

    func speechSynthesizer(_ synthesizer: AVSpeechSynthesizer, didCancel utterance: AVSpeechUtterance) {
        isSpeaking = false
    }
}

// MARK: - View

struct OCRResultView: View {
    let result: OCRResult
    @State private var showContinuous = false
    @StateObject private var speech = SpeechController()

    private var continuousText: String {
        result.blocks.map(\.text).joined()
    }

    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            HStack {
                Text("识别结果")
                    .font(.headline)
                Spacer()

                if showContinuous {
                    Button(action: {
                        if speech.isSpeaking {
                            speech.stop()
                        } else {
                            speech.speak(continuousText, language: "ja-JP")
                        }
                    }) {
                        Label(speech.isSpeaking ? "停止" : "朗读",
                              systemImage: speech.isSpeaking ? "stop.circle.fill" : "play.circle.fill")
                    }
                    .buttonStyle(.bordered)
                    .controlSize(.small)
                    .help(speech.isSpeaking ? "停止朗读" : "朗读原文")
                }

                Button(action: { showContinuous.toggle() }) {
                    Label(showContinuous ? "分段" : "连贯",
                          systemImage: showContinuous ? "list.bullet" : "text.alignleft")
                }
                .buttonStyle(.bordered)
                .controlSize(.small)
                .help(showContinuous ? "切换为逐行分段显示" : "切换为连贯完整文本")

                confidenceBadge
            }

            Divider()

            if showContinuous {
                ScrollView {
                    Text(continuousText)
                        .font(.body)
                        .textSelection(.enabled)
                        .frame(maxWidth: .infinity, alignment: .leading)
                        .padding(8)
                        .background(
                            RoundedRectangle(cornerRadius: 6)
                                .fill(.quaternary.opacity(0.3))
                        )
                }
            } else {
                ScrollView {
                    VStack(alignment: .leading, spacing: 8) {
                        ForEach(result.blocks) { block in
                            HStack(alignment: .top, spacing: 6) {
                                Text(block.text)
                                    .font(.body)
                                    .textSelection(.enabled)
                                Spacer()
                                Text(confidenceLabel(block.confidence))
                                    .font(.caption2)
                                    .foregroundStyle(confidenceColor(block.confidence))
                                    .monospacedDigit()
                            }
                            .padding(.vertical, 4)
                            .padding(.horizontal, 8)
                            .background(
                                RoundedRectangle(cornerRadius: 6)
                                    .fill(block.confidence < 0.7
                                        ? Color.yellow.opacity(0.15)
                                        : Color.clear)
                            )

                            if block != result.blocks.last {
                                Divider()
                            }
                        }
                    }
                }

                if !result.lowConfidenceBlocks.isEmpty {
                    HStack(spacing: 4) {
                        Image(systemName: "exclamationmark.triangle.fill")
                            .font(.caption)
                            .foregroundStyle(.yellow)
                        Text("黄色标记部分置信度较低，请核对")
                            .font(.caption)
                            .foregroundStyle(.secondary)
                    }
                }
            }
        }
        .padding()
    }

    private var confidenceBadge: some View {
        let pct = Int(result.averageConfidence * 100)
        return Text("平均置信度 \(pct)%")
            .font(.caption)
            .foregroundStyle(.secondary)
            .padding(.horizontal, 8)
            .padding(.vertical, 3)
            .background(
                Capsule()
                    .fill(confidenceColor(result.averageConfidence).opacity(0.15))
            )
    }

    private func confidenceLabel(_ value: Float) -> String {
        String(format: "%.0f%%", value * 100)
    }

    private func confidenceColor(_ value: Float) -> Color {
        switch value {
        case 0.8...1.0: return .green
        case 0.6..<0.8: return .orange
        default: return .red
        }
    }
}
