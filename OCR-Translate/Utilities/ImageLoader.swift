import AppKit
import UniformTypeIdentifiers

final class ImageLoader {
    @MainActor
    func loadFromFile() async -> NSImage? {
        let panel = NSOpenPanel()
        panel.allowedContentTypes = [.png, .jpeg, .heic, .bmp, .tiff]
        panel.allowsMultipleSelection = false
        panel.canChooseDirectories = false
        panel.message = "选择需要 OCR 识别的图片"

        guard case .OK = await panel.begin(), let url = panel.url else { return nil }
        return NSImage(contentsOf: url)
    }

    func loadFromURL(_ url: URL) -> NSImage? {
        NSImage(contentsOf: url)
    }

    func loadFromData(_ data: Data) -> NSImage? {
        NSImage(data: data)
    }
}
