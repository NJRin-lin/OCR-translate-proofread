import AppKit
import Vision

enum OCRError: LocalizedError {
    case imageConversionFailed
    case recognitionFailed
    case noTextFound

    var errorDescription: String? {
        switch self {
        case .imageConversionFailed: "图片转换失败"
        case .recognitionFailed: "文字识别失败"
        case .noTextFound: "未识别到任何文字"
        }
    }
}

final class OCRService {
    func recognize(_ image: NSImage) async throws -> OCRResult {
        guard let cgImage = image.cgImage(forProposedRect: nil, context: nil, hints: nil) else {
            throw OCRError.imageConversionFailed
        }

        let request = VNRecognizeTextRequest()
        request.recognitionLevel = .accurate
        request.usesLanguageCorrection = true
        request.recognitionLanguages = ["ja", "zh-Hans", "zh-Hant", "en"]
        request.minimumTextHeight = 0.01

        return try await withCheckedThrowingContinuation { continuation in
            let requestHandler = VNImageRequestHandler(cgImage: cgImage, options: [:])
            do {
                try requestHandler.perform([request])
                guard let observations = request.results, !observations.isEmpty else {
                    continuation.resume(throwing: OCRError.noTextFound)
                    return
                }

                let imageHeight = CGFloat(cgImage.height)
                let imageWidth = CGFloat(cgImage.width)

                let blocks: [OCRTextBlock] = observations.compactMap { observation in
                    guard let candidate = observation.topCandidates(1).first else { return nil }
                    let boundingBox = VNImageRectForNormalizedRect(
                        observation.boundingBox,
                        Int(imageWidth),
                        Int(imageHeight)
                    )
                    return OCRTextBlock(
                        text: candidate.string,
                        confidence: candidate.confidence,
                        boundingBox: boundingBox
                    )
                }

                let fullText = blocks.map(\.text).joined(separator: "\n")
                let avgConfidence = blocks.isEmpty ? 0 :
                    blocks.map(\.confidence).reduce(0, +) / Float(blocks.count)

                continuation.resume(returning: OCRResult(
                    blocks: blocks,
                    fullText: fullText,
                    averageConfidence: avgConfidence
                ))
            } catch {
                continuation.resume(throwing: OCRError.recognitionFailed)
            }
        }
    }
}
