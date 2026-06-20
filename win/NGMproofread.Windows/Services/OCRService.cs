using System.IO;
using System.Windows.Media.Imaging;
using NGMproofread.Windows.Models;

namespace NGMproofread.Windows.Services;

// Using extern alias or full global:: paths for WinRT types to avoid
// namespace conflict with NGMproofread.Windows
using WinBitmapDecoder = global::Windows.Graphics.Imaging.BitmapDecoder;
using WinBitmapPixelFormat = global::Windows.Graphics.Imaging.BitmapPixelFormat;
using WinBitmapAlphaMode = global::Windows.Graphics.Imaging.BitmapAlphaMode;
using WinSoftwareBitmap = global::Windows.Graphics.Imaging.SoftwareBitmap;
using WinOcrEngine = global::Windows.Media.Ocr.OcrEngine;
using WinOcrLine = global::Windows.Media.Ocr.OcrLine;
using WinLanguage = global::Windows.Globalization.Language;
using WinStorageFile = global::Windows.Storage.StorageFile;
using WinFileAccessMode = global::Windows.Storage.FileAccessMode;
using WinRect = global::Windows.Foundation.Rect;

public class OCRService
{
    /// <summary>
    /// Recognize text from a BitmapSource using Windows built-in OCR.
    /// Supports: Japanese, Chinese Simplified/Chinese Traditional, English.
    /// </summary>
    public async Task<OCRResult> RecognizeAsync(BitmapSource bitmapSource)
    {
        var tempPath = System.IO.Path.GetTempFileName() + ".png";

        try
        {
            // Save WPF bitmap as PNG temp file
            using (var fileStream = new FileStream(tempPath, FileMode.Create))
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                encoder.Save(fileStream);
            }

            // Load into WinRT SoftwareBitmap
            var file = await WinStorageFile.GetFileFromPathAsync(tempPath);
            using var stream = await file.OpenAsync(WinFileAccessMode.Read);
            var decoder = await WinBitmapDecoder.CreateAsync(stream);
            var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

            // Convert to Bgra8 if needed
            if (softwareBitmap.BitmapPixelFormat != WinBitmapPixelFormat.Bgra8 ||
                softwareBitmap.BitmapAlphaMode != WinBitmapAlphaMode.Premultiplied)
            {
                softwareBitmap = WinSoftwareBitmap.Convert(
                    softwareBitmap, WinBitmapPixelFormat.Bgra8, WinBitmapAlphaMode.Premultiplied);
            }

            // Create OCR engine for Japanese with fallback
            var engine = WinOcrEngine.TryCreateFromLanguage(new WinLanguage("ja"))
                ?? WinOcrEngine.TryCreateFromUserProfileLanguages();

            if (engine == null)
                throw new InvalidOperationException("无法创建 OCR 引擎，系统不支持文字识别");

            var result = await engine.RecognizeAsync(softwareBitmap);

            var blocks = result.Lines.Select(line =>
            {
                var confidence = EstimateConfidence(line);
                var rect = line.Words.Count > 0
                    ? line.Words[0].BoundingRect
                    : new WinRect(0, 0, 0, 0);

                return new OCRTextBlock
                {
                    Text = line.Text,
                    Confidence = confidence,
                    BoundingBox = new System.Drawing.RectangleF(
                        (float)rect.X, (float)rect.Y,
                        (float)rect.Width, (float)rect.Height)
                };
            }).ToList();

            return new OCRResult { Blocks = blocks };
        }
        finally
        {
            try { File.Delete(tempPath); } catch { }
        }
    }

    private static float EstimateConfidence(WinOcrLine line)
    {
        var hasJapanese = line.Text.Any(c =>
            (c >= 0x3040 && c <= 0x309F) ||
            (c >= 0x30A0 && c <= 0x30FF) ||
            (c >= 0x4E00 && c <= 0x9FFF));

        var baseConf = hasJapanese ? 0.85f : 0.65f;
        var lenBonus = Math.Min(line.Text.Length / 10f * 0.05f, 0.1f);
        return Math.Min(baseConf + lenBonus, 0.98f);
    }
}
