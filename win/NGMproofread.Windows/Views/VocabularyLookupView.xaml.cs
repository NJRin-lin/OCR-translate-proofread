using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using NGMproofread.Windows.Services;

namespace NGMproofread.Windows.Views;

public partial class VocabularyLookupView : UserControl
{
    private readonly VocabularyService _service = new();
    private VocabEntry? _result;
    private bool _isExpanded = true;
    private bool _isLoading;
    private string _externalQuery = "";

    public VocabularyLookupView()
    {
        InitializeComponent();
    }

    public void FocusSearchBox()
    {
        SearchBox.Focus();
        SearchBox.SelectAll();
    }

    public void SetExternalQuery(string word)
    {
        if (string.IsNullOrEmpty(word)) return;
        _externalQuery = word;
        SearchBox.Text = word;
        PerformLookup();
    }

    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            PerformLookup();
            e.Handled = true;
        }
    }

    private void ClearBtn_Click(object sender, RoutedEventArgs e)
    {
        SearchBox.Text = "";
        _result = null;
        _isExpanded = false;
        UpdateUI();
    }

    private async void SearchBtn_Click(object sender, RoutedEventArgs e)
    {
        await PerformLookupAsync();
    }

    private async void PerformLookup()
    {
        await PerformLookupAsync();
    }

    private async Task PerformLookupAsync()
    {
        var query = SearchBox.Text.Trim();
        if (string.IsNullOrEmpty(query)) return;

        _isLoading = true;
        _result = null;
        _isExpanded = true;
        UpdateUI();

        try
        {
            _result = await _service.LookupAsync(query);
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
        }

        _isLoading = false;
        UpdateUI();
    }

    private void ToggleBtn_Click(object sender, RoutedEventArgs e)
    {
        _isExpanded = !_isExpanded;
        UpdateUI();
    }

    private void UpdateUI()
    {
        ToggleBtn.Visibility = _result != null ? Visibility.Visible : Visibility.Collapsed;
        ToggleBtn.Content = _isExpanded ? "收起" : "展开";
        ClearBtn.Visibility = !string.IsNullOrEmpty(SearchBox.Text) ? Visibility.Visible : Visibility.Collapsed;

        SearchBtn.Visibility = _isLoading ? Visibility.Collapsed : Visibility.Visible;
        SearchProgress.Visibility = _isLoading ? Visibility.Visible : Visibility.Collapsed;

        if (_result != null && _isExpanded)
        {
            ResultCard.Visibility = Visibility.Visible;
            RenderResult();
        }
        else
        {
            ResultCard.Visibility = Visibility.Collapsed;
        }
    }

    private void RenderResult()
    {
        if (_result == null) return;

        ResultContent.Children.Clear();

        // Head: word + reading + tags
        var headStack = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };

        headStack.Children.Add(new TextBlock
        {
            Text = _result.Word, FontSize = 14, FontWeight = FontWeights.Bold,
            FontFamily = new FontFamily("Meiryo, MS Gothic, Yu Gothic")
        });

        headStack.Children.Add(new TextBlock
        {
            Text = $" ({_result.Reading})", FontSize = 11,
            Foreground = new SolidColorBrush(Colors.Gray),
            VerticalAlignment = VerticalAlignment.Center
        });

        if (!string.IsNullOrEmpty(_result.PartOfSpeech))
        {
            var posTag = CreateCapsule(_result.PartOfSpeech,
                new SolidColorBrush(Color.FromArgb(0x1A, 0x00, 0x7A, 0xFF)),
                new SolidColorBrush(Color.FromRgb(0x00, 0x7A, 0xFF)));
            headStack.Children.Add(posTag);
        }

        if (!string.IsNullOrEmpty(_result.JlptLevel))
        {
            var jlptTag = CreateCapsule(_result.JlptLevel,
                new SolidColorBrush(Color.FromArgb(0x1A, 0xFF, 0x95, 0x00)),
                new SolidColorBrush(Color.FromRgb(0xFF, 0x95, 0x00)));
            headStack.Children.Add(jlptTag);
        }

        ResultContent.Children.Add(headStack);

        // Meaning
        ResultContent.Children.Add(new TextBlock
        {
            Text = _result.Meaning, FontSize = 13,
            TextWrapping = TextWrapping.Wrap
        });

        // Notes
        if (!string.IsNullOrEmpty(_result.Notes))
        {
            ResultContent.Children.Add(new TextBlock
            {
                Text = _result.Notes, FontSize = 11,
                Foreground = new SolidColorBrush(Colors.Gray),
                Margin = new Thickness(0, 4, 0, 0)
            });
        }

        // Examples
        if (_result.Examples.Count > 0)
        {
            var exStack = new StackPanel { Margin = new Thickness(0, 6, 0, 0) };
            foreach (var ex in _result.Examples)
            {
                var exRow = new StackPanel { Orientation = Orientation.Horizontal };
                exRow.Children.Add(new TextBlock
                {
                    Text = "·", FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xAF, 0x52, 0xDE))
                });
                exRow.Children.Add(new TextBlock
                {
                    Text = ex, FontSize = 11,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(4, 0, 0, 0)
                });
                exStack.Children.Add(exRow);
            }
            ResultContent.Children.Add(exStack);
        }
    }

    private static Border CreateCapsule(string text, SolidColorBrush bg, SolidColorBrush fg)
    {
        return new Border
        {
            CornerRadius = new CornerRadius(8),
            Background = bg,
            Padding = new Thickness(5, 1, 5, 1),
            Margin = new Thickness(4, 0, 0, 0),
            Child = new TextBlock { Text = text, FontSize = 10, Foreground = fg }
        };
    }
}
