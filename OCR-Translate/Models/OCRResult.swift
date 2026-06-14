import Foundation
import CoreGraphics

struct OCRTextBlock: Identifiable, Equatable {
    let id = UUID()
    let text: String
    let confidence: Float
    let boundingBox: CGRect
}

struct OCRResult: Equatable {
    let blocks: [OCRTextBlock]
    let fullText: String
    let averageConfidence: Float

    var highConfidenceBlocks: [OCRTextBlock] {
        blocks.filter { $0.confidence >= 0.7 }
    }

    var lowConfidenceBlocks: [OCRTextBlock] {
        blocks.filter { $0.confidence < 0.7 }
    }
}
