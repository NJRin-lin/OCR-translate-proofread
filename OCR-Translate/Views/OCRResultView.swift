import SwiftUI

struct OCRResultView: View {
    let result: OCRResult

    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            HStack {
                Text("识别结果")
                    .font(.headline)
                Spacer()
                confidenceBadge
            }

            Divider()

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
