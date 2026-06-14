import Foundation

enum DeepSeekError: LocalizedError {
    case invalidURL
    case noAPIKey
    case requestFailed(Int)
    case decodingFailed
    case emptyResponse

    var errorDescription: String? {
        switch self {
        case .invalidURL: "API 地址无效"
        case .noAPIKey: "请先在设置中配置 API Key"
        case .requestFailed(let code): "API 请求失败 (HTTP \(code))"
        case .decodingFailed: "API 响应解析失败"
        case .emptyResponse: "API 返回内容为空"
        }
    }
}

struct ChatMessage: Codable {
    let role: String
    let content: String
}

struct ChatRequest: Codable {
    let model: String
    let messages: [ChatMessage]
    let temperature: Double
    let maxTokens: Int

    enum CodingKeys: String, CodingKey {
        case model, messages, temperature
        case maxTokens = "max_tokens"
    }
}

struct ChatChoice: Codable {
    let message: ChatMessage
    let finishReason: String?

    enum CodingKeys: String, CodingKey {
        case message
        case finishReason = "finish_reason"
    }
}

struct ChatResponse: Codable {
    let choices: [ChatChoice]
}

final class DeepSeekService {
    private let baseURL = "https://api.deepseek.com/v1/chat/completions"
    private let model = "deepseek-chat"
    private let store = APIKeyStore()

    func chat(
        systemPrompt: String,
        userMessage: String,
        temperature: Double = 0.3,
        maxTokens: Int = 4096
    ) async throws -> String {
        guard let apiKey = store.read(), !apiKey.isEmpty else {
            throw DeepSeekError.noAPIKey
        }

        guard let url = URL(string: baseURL) else {
            throw DeepSeekError.invalidURL
        }

        let messages: [ChatMessage] = [
            ChatMessage(role: "system", content: systemPrompt),
            ChatMessage(role: "user", content: userMessage)
        ]

        let requestBody = ChatRequest(
            model: model,
            messages: messages,
            temperature: temperature,
            maxTokens: maxTokens
        )

        var urlRequest = URLRequest(url: url)
        urlRequest.httpMethod = "POST"
        urlRequest.setValue("application/json", forHTTPHeaderField: "Content-Type")
        urlRequest.setValue("Bearer \(apiKey)", forHTTPHeaderField: "Authorization")
        urlRequest.httpBody = try JSONEncoder().encode(requestBody)
        urlRequest.timeoutInterval = 60

        let (data, response) = try await URLSession.shared.data(for: urlRequest)

        guard let httpResponse = response as? HTTPURLResponse else {
            throw DeepSeekError.requestFailed(0)
        }

        guard httpResponse.statusCode == 200 else {
            throw DeepSeekError.requestFailed(httpResponse.statusCode)
        }

        guard let chatResponse = try? JSONDecoder().decode(ChatResponse.self, from: data),
              let content = chatResponse.choices.first?.message.content,
              !content.isEmpty else {
            throw DeepSeekError.emptyResponse
        }

        return content
    }
}
