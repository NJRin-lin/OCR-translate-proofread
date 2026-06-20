using System.Windows.Controls;
using NGMproofread.Windows.Models;

namespace NGMproofread.Windows.Views;

public partial class TranslationResultView : UserControl
{
    public TranslationResultView()
    {
        InitializeComponent();
    }

    public void SetResult(TranslationResult result, string originalText)
    {
        var cleanedOriginal = originalText
            .Replace("\n", "")
            .Replace(" ", "")
            .Trim();

        OriginalText.Text = cleanedOriginal;
        TranslatedText.Text = result.TranslatedText;
    }
}
