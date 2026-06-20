import Foundation

final class APIKeyStore {
    private let defaults = UserDefaults.standard

    func key(for provider: AIProvider) -> String {
        "api_key_\(provider.rawValue)"
    }

    func providerKey(for provider: AIProvider) -> String {
        "active_provider"
    }

    func save(_ value: String, for provider: AIProvider) {
        defaults.set(value.trimmingCharacters(in: .whitespaces), forKey: key(for: provider))
    }

    func read(for provider: AIProvider) -> String? {
        defaults.string(forKey: key(for: provider))
    }

    func hasKey(for provider: AIProvider) -> Bool {
        guard let stored = read(for: provider) else { return false }
        return !stored.isEmpty
    }

    func delete(for provider: AIProvider) {
        defaults.removeObject(forKey: key(for: provider))
    }

    // MARK: - Active provider

    var activeProvider: AIProvider {
        get {
            guard let raw = defaults.string(forKey: providerKey(for: .deepseek)),
                  let provider = AIProvider(rawValue: raw) else {
                return .deepseek // default
            }
            return provider
        }
        set {
            defaults.set(newValue.rawValue, forKey: providerKey(for: .deepseek))
        }
    }

    /// Read key for the currently active provider
    func readActiveKey() -> String? {
        read(for: activeProvider)
    }

    func hasActiveKey() -> Bool {
        hasKey(for: activeProvider)
    }
}
