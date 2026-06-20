using System.Windows;
using System.Windows.Controls;
using NGMproofread.Windows.Models;

namespace NGMproofread.Windows.Views;

public partial class TranslationResultView : UserControl
{
    public event EventHandler<string>? ReTranslateRequested;

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

    private void ReTranslateBtn_Click(object sender, RoutedEventArgs e)
    {
        var editedText = OriginalText.Text.Trim();
        if (!string.IsNullOrEmpty(editedText))
            ReTranslateRequested?.Invoke(this, editedText);
    }
}
