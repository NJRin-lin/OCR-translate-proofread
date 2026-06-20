using System.Drawing;

namespace NGMproofread.Windows.Models;

public class OCRTextBlock
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Text { get; init; } = "";
    public float Confidence { get; init; }
    public RectangleF BoundingBox { get; init; }
}

public class OCRResult
{
    public List<OCRTextBlock> Blocks { get; init; } = [];

    public string FullText => string.Concat(Blocks.Select(b => b.Text));

    public float AverageConfidence =>
        Blocks.Count > 0 ? Blocks.Average(b => b.Confidence) : 0f;

    public List<OCRTextBlock> LowConfidenceBlocks =>
        Blocks.Where(b => b.Confidence < 0.7f).ToList();
}
