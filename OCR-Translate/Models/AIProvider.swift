import Foundation

enum AIProvider: String, CaseIterable, Equatable {
    case deepseek
    case openai
    case gemini

    var displayName: String {
        switch self {
        case .deepseek: "DeepSeek"
        case .openai:   "ChatGPT (OpenAI)"
        case .gemini:   "Gemini (Google)"
        }
    }

    var shortName: String {
        switch self {
        case .deepseek: "DeepSeek"
        case .openai:   "OpenAI"
        case .gemini:   "Gemini"
        }
    }

    var endpoint: String {
        switch self {
        case .deepseek: "https://api.deepseek.com/v1/chat/completions"
        case .openai:   "https://api.openai.com/v1/chat/completions"
        case .gemini:   "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent"
        }
    }

    var model: String {
        switch self {
        case .deepseek: "deepseek-chat"
        case .openai:   "gpt-4o"
        case .gemini:   "gemini-2.0-flash"
        }
    }

    /// How the API key is sent
    var authHeader: (String, String)? {
        switch self {
        case .deepseek, .openai:
            return ("Authorization", "Bearer <key>")
        case .gemini:
            return nil // key goes in URL query param
        }
    }

    var getKeyURL: String {
        switch self {
        case .deepseek: "platform.deepseek.com → API Keys"
        case .openai:   "platform.openai.com → API Keys"
        case .gemini:   "aistudio.google.com → Get API Key"
        }
    }

    /// Key prefix hint for placeholder
    var keyPrefix: String {
        switch self {
        case .deepseek: "sk-"
        case .openai:   "sk-"
        case .gemini:   "AIza"
        }
    }
}
