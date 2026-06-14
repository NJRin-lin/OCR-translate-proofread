import AppKit
import Foundation

enum ScreenshotError: LocalizedError {
    case captureFailed
    case noImageReturned

    var errorDescription: String? {
        switch self {
        case .captureFailed: "截图失败，请重试"
        case .noImageReturned: "未获取到截图图片"
        }
    }
}

final class ScreenshotManager {
    func capture() async throws -> NSImage {
        let tempURL = FileManager.default.temporaryDirectory
            .appendingPathComponent("ocr_screenshot_\(UUID().uuidString).png")

        let process = Process()
        process.executableURL = URL(fileURLWithPath: "/usr/sbin/screencapture")
        process.arguments = ["-i", "-t", "png", tempURL.path]
        try await run(process)

        guard FileManager.default.fileExists(atPath: tempURL.path),
              let image = NSImage(contentsOf: tempURL) else {
            throw ScreenshotError.noImageReturned
        }

        try? FileManager.default.removeItem(at: tempURL)
        return image
    }

    func captureFromClipboard() -> NSImage? {
        guard NSPasteboard.general.canReadItem(withDataConformingToTypes: NSImage.imageTypes) else {
            return nil
        }
        return NSImage(pasteboard: NSPasteboard.general)
    }

    private func run(_ process: Process) async throws {
        try await withCheckedThrowingContinuation { (continuation: CheckedContinuation<Void, Error>) in
            process.terminationHandler = { proc in
                if proc.terminationStatus == 0 {
                    continuation.resume()
                } else {
                    continuation.resume(throwing: ScreenshotError.captureFailed)
                }
            }
            do {
                try process.run()
            } catch {
                continuation.resume(throwing: error)
            }
        }
    }
}
