using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NGMproofread.Windows.Models;
using NGMproofread.Windows.Utilities;

namespace NGMproofread.Windows.Views;

public partial class SettingsWindow : Window
{
    private readonly APIKeyStore _store;
    private readonly GlossaryStore _glossary;
    private AIProvider _selectedProvider;
    private bool _showKey;
    private string _currentKey = "";

    public AnalysisMode AnalysisMode { get; private set; } = AnalysisMode.Study;

    public SettingsWindow(APIKeyStore apiKeyStore, GlossaryStore glossary)
    {
        InitializeComponent();
        _store = apiKeyStore;
        _glossary = glossary;
        _selectedProvider = _store.ActiveProvider;

        // Restore provider selection
        switch (_selectedProvider)
        {
            case AIProvider.OpenAI: OpenAIRadio.IsChecked = true; break;
            case AIProvider.Gemini: GeminiRadio.IsChecked = true; break;
            default: DeepSeekRadio.IsChecked = true; break;
        }

        // Restore analysis mode (from settings or default)
        if (_store.DefaultAnalysisMode == AnalysisMode.Proofread)
            ProofreadModeRadio.IsChecked = true;
        else
            StudyModeRadio.IsChecked = true;

        UpdateProviderUI();
        LoadGlossary();

        // Provider change events
        DeepSeekRadio.Checked += (_, _) => { _selectedProvider = AIProvider.DeepSeek; OnProviderChanged(); };
        OpenAIRadio.Checked += (_, _) => { _selectedProvider = AIProvider.OpenAI; OnProviderChanged(); };
        GeminiRadio.Checked += (_, _) => { _selectedProvider = AIProvider.Gemini; OnProviderChanged(); };

        // TextBox focus events for placeholders
        NewWordBox.GotFocus += (_, _) => { if (NewWordBox.Text == "日语词汇") NewWordBox.Text = ""; NewWordBox.Foreground = SystemColors.WindowTextBrush; };
        NewWordBox.LostFocus += (_, _) => { if (string.IsNullOrWhiteSpace(NewWordBox.Text)) { NewWordBox.Text = "日语词汇"; NewWordBox.Foreground = Brushes.Gray; } };
        NewTranslationBox.GotFocus += (_, _) => { if (NewTranslationBox.Text == "中文翻译") NewTranslationBox.Text = ""; NewTranslationBox.Foreground = SystemColors.WindowTextBrush; };
        NewTranslationBox.LostFocus += (_, _) => { if (string.IsNullOrWhiteSpace(NewTranslationBox.Text)) { NewTranslationBox.Text = "中文翻译"; NewTranslationBox.Foreground = Brushes.Gray; } };
    }

    private void OnProviderChanged()
    {
        _store.ActiveProvider = _selectedProvider;
        UpdateProviderUI();
    }

    private void UpdateProviderUI()
    {
        APIKeyLabel.Text = $"{_selectedProvider.ShortName()} API Key";
        _currentKey = _store.Read(_selectedProvider) ?? "";
        _showKey = false;
        APIKeyBox.Text = string.IsNullOrEmpty(_currentKey) ? "" : new string('•', _currentKey.Length);
        SaveStatusText.Text = "";
    }

    private void ToggleKeyVisibilityBtn_Click(object sender, RoutedEventArgs e)
    {
        _showKey = !_showKey;
        if (_showKey)
        {
            APIKeyBox.Text = _currentKey;
        }
        else
        {
            _currentKey = APIKeyBox.Text;
            APIKeyBox.Text = string.IsNullOrEmpty(_currentKey) ? "" : new string('•', _currentKey.Length);
        }
    }

    private void SaveKeyBtn_Click(object sender, RoutedEventArgs e)
    {
        var key = (_showKey ? APIKeyBox.Text : _currentKey).Trim();
        if (string.IsNullOrEmpty(key)) return;
        _currentKey = key;

        try
        {
            _store.Save(key, _selectedProvider);
            SaveStatusText.Text = $"{_selectedProvider.ShortName()} 已保存";
            SaveStatusText.Foreground = new SolidColorBrush(Color.FromRgb(0x34, 0xC7, 0x59));
        }
        catch (Exception ex)
        {
            SaveStatusText.Text = $"保存失败: {ex.Message}";
            SaveStatusText.Foreground = new SolidColorBrush(Colors.Red);
            return;
        }

        // Auto-dismiss after 2 seconds
        _ = Task.Run(async () =>
        {
            await Task.Delay(2000);
            Dispatcher.Invoke(() => SaveStatusText.Text = "");
        });
    }

    private void DeleteKeyBtn_Click(object sender, RoutedEventArgs e)
    {
        _store.Delete(_selectedProvider);
        APIKeyBox.Text = "";
        SaveStatusText.Text = "已删除";
        SaveStatusText.Foreground = new SolidColorBrush(Color.FromRgb(0x34, 0xC7, 0x59));

        _ = Task.Run(async () =>
        {
            await Task.Delay(2000);
            Dispatcher.Invoke(() => SaveStatusText.Text = "");
        });
    }

    private void AddGlossaryBtn_Click(object sender, RoutedEventArgs e)
    {
        var word = NewWordBox.Text.Trim();
        var translation = NewTranslationBox.Text.Trim();

        if (word == "日语词汇") word = "";
        if (translation == "中文翻译") translation = "";

        if (string.IsNullOrEmpty(word) || string.IsNullOrEmpty(translation))
            return;

        _glossary.Add(word, translation);
        NewWordBox.Text = "日语词汇";
        NewWordBox.Foreground = Brushes.Gray;
        NewTranslationBox.Text = "中文翻译";
        NewTranslationBox.Foreground = Brushes.Gray;
        LoadGlossary();
    }

    private void RemoveGlossaryBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is GlossaryDisplayItem item)
        {
            _glossary.Remove(item.Entry);
            LoadGlossary();
        }
    }

    private void LoadGlossary()
    {
        var items = _glossary.Entries.Select(e => new GlossaryDisplayItem
        {
            Display = $"「{e.Word}」→「{e.Translation}」",
            Entry = e
        }).ToList();

        GlossaryList.ItemsSource = items;
        NoGlossaryHint.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void DoneBtn_Click(object sender, RoutedEventArgs e)
    {
        AnalysisMode = StudyModeRadio.IsChecked == true ? AnalysisMode.Study : AnalysisMode.Proofread;
        _store.DefaultAnalysisMode = AnalysisMode;
        Close();
    }
}

public class GlossaryDisplayItem
{
    public string Display { get; set; } = "";
    public GlossaryEntry Entry { get; set; } = null!;
}
