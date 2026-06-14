import SwiftUI

@main
struct OCRTranslateApp: App {
    var body: some Scene {
        WindowGroup {
            ContentView()
                .frame(minWidth: 1000, minHeight: 650)
        }
        .windowStyle(.titleBar)
        .windowResizability(.contentMinSize)
        .defaultSize(width: 1200, height: 750)

        Settings {
            SettingsView(analysisMode: .constant(.proofread))
        }
    }
}
