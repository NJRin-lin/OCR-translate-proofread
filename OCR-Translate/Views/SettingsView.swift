import SwiftUI

struct SettingsView: View {
    @Binding var analysisMode: AnalysisMode
    @Environment(\.dismiss) private var dismiss

    @State private var apiKey: String = ""
    @State private var showKey: Bool = false
    @State private var saveStatus: String?

    private let store = APIKeyStore()

    var body: some View {
        VStack(alignment: .leading, spacing: 0) {
            HStack {
                Text("设置")
                    .font(.title2)
                    .bold()
                Spacer()
                Button("完成") { dismiss() }
                    .buttonStyle(.borderedProminent)
                    .controlSize(.small)
            }
            .padding()

            Divider()

            ScrollView {
                VStack(alignment: .leading, spacing: 24) {
                    apiKeySection
                    Divider()
                    analysisModeSection
                    Divider()
                    aboutSection
                }
                .padding()
            }
        }
        .frame(width: 480, height: 420)
        .onAppear { apiKey = store.read() ?? "" }
    }

    // MARK: - API Key Section

    private var apiKeySection: some View {
        VStack(alignment: .leading, spacing: 10) {
            HStack {
                Image(systemName: "key.fill")
                    .foregroundStyle(.blue)
                Text("DeepSeek API Key")
                    .font(.headline)
            }

            Text("API Key 仅存储在本机")
                .font(.caption)
                .foregroundStyle(.secondary)

            HStack {
                if showKey {
                    TextField("sk-xxxxxxxx", text: $apiKey)
                        .textFieldStyle(.roundedBorder)
                        .font(.body.monospaced())
                } else {
                    SecureField("sk-xxxxxxxx", text: $apiKey)
                        .textFieldStyle(.roundedBorder)
                }

                Button(action: { showKey.toggle() }) {
                    Image(systemName: showKey ? "eye.slash" : "eye")
                }
                .buttonStyle(.borderless)
                .help(showKey ? "隐藏" : "显示")
            }

            HStack {
                Button("保存") {
                    let trimmed = apiKey.trimmingCharacters(in: .whitespaces)
                    guard !trimmed.isEmpty else { return }
                    store.save(trimmed)
                    saveStatus = "已保存"
                    DispatchQueue.main.asyncAfter(deadline: .now() + 2) { saveStatus = nil }
                }
                .buttonStyle(.borderedProminent)
                .controlSize(.small)
                .disabled(apiKey.trimmingCharacters(in: .whitespaces).isEmpty)

                Button("删除") {
                    store.delete()
                    apiKey = ""
                    saveStatus = "已删除"
                    DispatchQueue.main.asyncAfter(deadline: .now() + 2) { saveStatus = nil }
                }
                .buttonStyle(.bordered)
                .controlSize(.small)
                .tint(.red)

                if let status = saveStatus {
                    Text(status)
                        .font(.caption)
                        .foregroundStyle(.green)
                }
            }

            Text("获取 API Key: platform.deepseek.com → API Keys")
                .font(.caption)
                .foregroundStyle(.tertiary)
        }
    }

    // MARK: - Analysis Mode

    private var analysisModeSection: some View {
        VStack(alignment: .leading, spacing: 10) {
            HStack {
                Image(systemName: "text.magnifyingglass")
                    .foregroundStyle(.purple)
                Text("默认分析深度")
                    .font(.headline)
            }

            Picker("", selection: $analysisMode) {
                Text("详细分析 — 完整句子拆解 + 逐词注解").tag(AnalysisMode.detailed)
                Text("简洁分析 — 仅关键语法 + 罕见词汇").tag(AnalysisMode.concise)
            }
            .pickerStyle(.radioGroup)
        }
    }

    // MARK: - About

    private var aboutSection: some View {
        VStack(alignment: .leading, spacing: 8) {
            HStack {
                Image(systemName: "info.circle.fill")
                    .foregroundStyle(.gray)
                Text("关于")
                    .font(.headline)
            }

            VStack(alignment: .leading, spacing: 4) {
                Text("OCR 翻译学习工具")
                    .font(.body)
                Text("版本 1.0.0")
                    .font(.caption)
                    .foregroundStyle(.secondary)
                Text("使用 macOS Vision 框架进行 OCR 识别")
                    .font(.caption)
                    .foregroundStyle(.secondary)
                Text("翻译与分析由 DeepSeek 提供支持")
                    .font(.caption)
                    .foregroundStyle(.secondary)
            }
        }
    }

}
