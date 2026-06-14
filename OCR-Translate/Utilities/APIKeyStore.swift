import Foundation

final class APIKeyStore {
    private let defaults = UserDefaults.standard
    private let key = "deepseek_api_key"

    func save(_ value: String) {
        defaults.set(value.trimmingCharacters(in: .whitespaces), forKey: key)
    }

    func read() -> String? {
        defaults.string(forKey: key)
    }

    func hasKey() -> Bool {
        guard let stored = read() else { return false }
        return !stored.isEmpty
    }

    func delete() {
        defaults.removeObject(forKey: key)
    }
}