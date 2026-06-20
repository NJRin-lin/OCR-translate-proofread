using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using NGMproofread.Windows.Models;
using NGMproofread.Windows.Services;
using NGMproofread.Windows.Utilities;

namespace NGMproofread.Windows.Views;

public partial class MainWindow : Window
{
    // State
    private BitmapSource? _selectedImage;
    private OCRResult? _ocrResult;
    private bool _isProcessingOCR;
    private bool _isProcessingAI;
    private string _errorMessage = "";
    private InputMode _inputMode = InputMode.Image;
    private string _manualText = "";
    private string _proofreadText = "";

    // Per-mode results
    private TranslationResult? _imageTranslation;
    private AnalysisResult? _imageAnalysis;
    private readonly Dictionary<AnalysisMode, AnalysisResult> _imageCache = [];
    private TranslationResult? _textTranslation;
    private AnalysisResult? _textAnalysis;
    private readonly Dictionary<AnalysisMode, AnalysisResult> _textCache = [];

    // Services (initialized in constructor due to dependency chain)
    private readonly OCRService _ocrService = new();
    private readonly TranslationService _translationService;
    private readonly AnalysisService _analysisService;
    private readonly AIService _aiService;
    private readonly VocabularyService _vocabularyService;
    private readonly ImageLoader _imageLoader = new();
    private readonly APIKeyStore _apiKeyStore = new();
    private readonly GlossaryStore _glossary = new();

    private AnalysisMode CurrentAnalysisMode =>
        StudyModeBtn.IsChecked == true ? AnalysisMode.Study : AnalysisMode.Proofread;

    private bool HasAPIKey => _apiKeyStore.HasActiveKey();

    private string CurrentText
    {
        get
        {
            var text = _inputMode == InputMode.Text ? _manualText : (_ocrResult?.FullText ?? "");
            return text.Replace("\n", "").Replace(" ", "").Trim();
        }
    }

    private TranslationResult? CurrentTranslation =>
        _inputMode == InputMode.Image ? _imageTranslation : _textTranslation;

    private AnalysisResult? CurrentAnalysis =>
        _inputMode == InputMode.Image ? _imageAnalysis : _textAnalysis;

    private Dictionary<AnalysisMode, AnalysisResult> CurrentCache
    {
        get => _inputMode == InputMode.Image ? _imageCache : _textCache;
    }

    public MainWindow()
    {
        InitializeComponent();

        _aiService = new AIService(_apiKeyStore);
        _translationService = new TranslationService(_aiService);
        _analysisService = new AnalysisService(_aiService);
        _vocabularyService = new VocabularyService(_aiService);
        VocabLookupView.SetService(_vocabularyService);

        RestoreLayout();
        ApplySavedAnalysisMode();
        SetupEventHandlers();
        UpdateAllStates();

        Closing += (_, _) => SaveLayout();
    }

    private void RestoreLayout()
    {
        var sw = _apiKeyStore.SidebarWidth;
        var cw = _apiKeyStore.ContentWidth;
        if (sw > 0) SidebarColumn.Width = new GridLength(sw);
        if (cw > 0) ContentColumn.Width = new GridLength(cw);
    }

    private void SaveLayout()
    {
        _apiKeyStore.SidebarWidth = SidebarColumn.ActualWidth;
        _apiKeyStore.ContentWidth = ContentColumn.ActualWidth;
    }

    private void ApplySavedAnalysisMode()
    {
        if (_apiKeyStore.DefaultAnalysisMode == AnalysisMode.Proofread)
            ProofreadModeBtn.IsChecked = true;
        else
            StudyModeBtn.IsChecked = true;
    }

    private void SetupEventHandlers()
    {
        // Input mode switching
        ImageModeBtn.Checked += (_, _) => SwitchToImageMode();
        ImageModeBtn.Unchecked += (_, _) => SwitchToTextMode();

        // Analysis mode switching
        ProofreadModeBtn.Checked += (_, _) => OnAnalysisModeChanged();
        StudyModeBtn.Checked += (_, _) => OnAnalysisModeChanged();

        // Keyboard shortcut: Ctrl+K for vocab lookup
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.K && Keyboard.Modifiers == ModifierKeys.Control)
            {
                VocabLookupView.FocusSearchBox();
                e.Handled = true;
            }
        };

        // Croppable image callback
        CroppableView.ImageCropped += OnImageCropped;
        CroppableView.RequestNewImage += OnRequestNewImage;

        // Re-translate callback
        TranslationResultViewCtrl.ReTranslateRequested += OnReTranslateRequested;
    }

    private enum InputMode { Image, Text }

    // ── Input mode switching ──

    private void SwitchToImageMode()
    {
        if (_inputMode == InputMode.Image) return;
        _inputMode = InputMode.Image;
        CroppableView.Visibility = Visibility.Visible;
        TextInputPanel.Visibility = Visibility.Collapsed;
        TextModeRefPanel.Visibility = Visibility.Collapsed;
        UpdateAllStates();
    }

    private void SwitchToTextMode()
    {
        if (_inputMode == InputMode.Text) return;
        _inputMode = InputMode.Text;
        CroppableView.Visibility = Visibility.Collapsed;
        TextInputPanel.Visibility = Visibility.Visible;
        TextModeRefPanel.Visibility = Visibility.Visible;
        UpdateAllStates();
    }

    // ── Analysis mode change (triggers re-analysis with cache) ──

    private async void OnAnalysisModeChanged()
    {
        if (string.IsNullOrEmpty(CurrentText)) return;

        var mode = CurrentAnalysisMode;
        if (CurrentCache.TryGetValue(mode, out var cached))
        {
            ApplyAnalysis(cached);
            return;
        }

        _ = RunAsync(async () =>
        {
            _isProcessingAI = true;
            UpdateAllStates();

            if (CurrentTranslation == null)
            {
                if (_inputMode == InputMode.Text)
                    await PerformTextTranslationAsync();
                else
                    await PerformTranslationAndAnalysisAsync();
            }
            else
            {
                try
                {
                    var result = await _analysisService.AnalyzeAsync(CurrentText, mode);
                    ApplyAnalysis(result);
                }
                catch (Exception ex)
                {
                    _errorMessage = ex.Message;
                }
            }

            _isProcessingAI = false;
            UpdateAllStates();
        });
    }

    // ── Upload image / new image request ──

    private async void OnRequestNewImage(object? sender, EventArgs e) => await UploadImageAsync();

    private async void UploadBtn_Click(object sender, RoutedEventArgs e) => await UploadImageAsync();

    private async void UploadRefImageBtn_Click(object sender, RoutedEventArgs e) => await UploadRefImageAsync();

    private async Task UploadRefImageAsync()
    {
        var image = await _imageLoader.LoadFromFileAsync();
        if (image != null)
        {
            TextRefImageView.SetImage(image);
            TextRefEmptyHint.Visibility = Visibility.Collapsed;
        }
    }

    private async Task UploadImageAsync()
    {
        var image = await _imageLoader.LoadFromFileAsync();
        if (image != null)
        {
            _selectedImage = image;
            CroppableView.SetImage(image);
            ResetResults();
            UpdateAllStates();
        }
    }

    // ── Image cropped → OCR ──

    private async void OnImageCropped(object? sender, BitmapSource croppedImage)
    {
        await PerformOCRAsync(croppedImage);
    }

    private async Task PerformOCRAsync(BitmapSource image)
    {
        _isProcessingOCR = true;
        _errorMessage = "";
        _imageTranslation = null;
        _imageAnalysis = null;
        _imageCache.Clear();
        UpdateAllStates();

        try
        {
            var ocr = await _ocrService.RecognizeAsync(image);
            _ocrResult = ocr;

            if (string.IsNullOrEmpty(ocr.FullText))
            {
                _errorMessage = "未识别到文字内容";
            }
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
        }

        _isProcessingOCR = false;
        UpdateAllStates();
    }

    // ── Text mode: start translation ──

    private async void StartTextTranslationBtn_Click(object sender, RoutedEventArgs e)
    {
        _textTranslation = null;
        _textAnalysis = null;
        _textCache.Clear();
        _errorMessage = "";
        _isProcessingAI = true;
        UpdateAllStates();
        await PerformTextTranslationAsync();
        _isProcessingAI = false;
        UpdateAllStates();
    }

    private async Task PerformTextTranslationAsync()
    {
        var text = _manualText.Trim();
        if (string.IsNullOrEmpty(text)) return;

        try
        {
            var transTask = _translationService.TranslateAsync(text, _glossary.BuildPromptSnippet());
            var analTask = _analysisService.AnalyzeAsync(CurrentText, CurrentAnalysisMode);

            await Task.WhenAll(transTask, analTask);

            _textTranslation = transTask.Result;
            _textAnalysis = analTask.Result;
            _textCache[CurrentAnalysisMode] = analTask.Result;
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
        }
    }

    // ── Image mode: start translation ──

    private async void StartTranslationBtn_Click(object sender, RoutedEventArgs e)
    {
        _isProcessingAI = true;
        UpdateAllStates();
        await PerformTranslationAndAnalysisAsync();
        _isProcessingAI = false;
        UpdateAllStates();
    }

    private async Task PerformTranslationAndAnalysisAsync()
    {
        if (_ocrResult == null || string.IsNullOrEmpty(_ocrResult.FullText)) return;

        try
        {
            var transTask = _translationService.TranslateAsync(_ocrResult.FullText, _glossary.BuildPromptSnippet());
            var analTask = _analysisService.AnalyzeAsync(CurrentText, CurrentAnalysisMode);

            await Task.WhenAll(transTask, analTask);

            _imageTranslation = transTask.Result;
            _imageAnalysis = analTask.Result;
            _imageCache[CurrentAnalysisMode] = analTask.Result;
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
        }
    }

    // ── Re-translate from edited text ──

    private async void OnReTranslateRequested(object? sender, string editedText)
    {
        _isProcessingAI = true;
        UpdateAllStates();

        try
        {
            var transTask = _translationService.TranslateAsync(editedText, _glossary.BuildPromptSnippet());
            var analTask = _analysisService.AnalyzeAsync(editedText, CurrentAnalysisMode);

            await Task.WhenAll(transTask, analTask);

            if (_inputMode == InputMode.Image)
            {
                _imageTranslation = transTask.Result;
                _imageAnalysis = analTask.Result;
                _imageCache[CurrentAnalysisMode] = analTask.Result;
            }
            else
            {
                _textTranslation = transTask.Result;
                _textAnalysis = analTask.Result;
                _textCache[CurrentAnalysisMode] = analTask.Result;
            }
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
        }

        _isProcessingAI = false;
        UpdateAllStates();
    }

    // ── Clear text ──

    private void ClearTranslationBtn_Click(object sender, RoutedEventArgs e)
    {
        TranslationTextEditor.Text = "";
        _manualText = "";
        UpdateAllStates();
    }

    private void ClearProofreadBtn_Click(object sender, RoutedEventArgs e)
    {
        ProofreadTextEditor.Text = "";
        _proofreadText = "";
        UpdateAllStates();
    }

    private void ClearTextBtn_Click(object sender, RoutedEventArgs e)
    {
        TranslationTextEditor.Text = "";
        ProofreadTextEditor.Text = "";
        _manualText = "";
        _proofreadText = "";
        _textTranslation = null;
        _textAnalysis = null;
        _textCache.Clear();
        _errorMessage = "";
        UpdateAllStates();
    }

    // ── Retry OCR ──

    private async void RetryOCRBtn_Click(object sender, RoutedEventArgs e)
    {
        _errorMessage = "";
        if (_selectedImage != null)
            await PerformOCRAsync(_selectedImage);
        UpdateAllStates();
    }

    // ── Settings ──

    private void SettingsBtn_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SettingsWindow(_apiKeyStore, _glossary)
        {
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        dialog.ShowDialog();
        ApplySavedAnalysisMode();
        UpdateAllStates();
    }

    // ── State helpers ──

    private void ResetResults()
    {
        _ocrResult = null;
        _imageTranslation = null; _imageAnalysis = null; _imageCache.Clear();
        _textTranslation = null; _textAnalysis = null; _textCache.Clear();
        _errorMessage = "";
    }

    private void ApplyAnalysis(AnalysisResult result)
    {
        if (_inputMode == InputMode.Image)
        {
            _imageAnalysis = result;
            _imageCache[CurrentAnalysisMode] = result;
        }
        else
        {
            _textAnalysis = result;
            _textCache[CurrentAnalysisMode] = result;
        }
    }

    private void UpdateAllStates()
    {
        Dispatcher.Invoke(() =>
        {
            // AI progress bar
            AIProgressBar.Visibility = _isProcessingAI ? Visibility.Visible : Visibility.Collapsed;

            // Sync text editors
            _manualText = TranslationTextEditor.Text;
            _proofreadText = ProofreadTextEditor.Text;

            // Sidebar
            UpdateSidebarState();
            // Content column
            UpdateContentState();
            // Detail column
            UpdateDetailState();
        });
    }

    private void UpdateSidebarState()
    {
        // Text editors are bound via x:Name, sync is done on get
    }

    private void UpdateContentState()
    {
        if (_inputMode == InputMode.Text)
        {
            OCRContentPanel.Visibility = Visibility.Collapsed;
            TextModeRefPanel.Visibility = Visibility.Visible;
            OCRWarningBar.Visibility = Visibility.Collapsed;
            return;
        }

        // Image mode
        OCRContentPanel.Visibility = Visibility.Visible;
        TextModeRefPanel.Visibility = Visibility.Collapsed;
        ReferenceImageView.Visibility = Visibility.Collapsed;
        OCRWarningBar.Visibility = _ocrResult != null ? Visibility.Visible : Visibility.Collapsed;

        if (_isProcessingOCR)
        {
            OCRLoadingPanel.Visibility = Visibility.Visible;
            OCRErrorPanel.Visibility = Visibility.Collapsed;
            OCRResultViewCtrl.Visibility = Visibility.Collapsed;
            OCREmptyText.Visibility = Visibility.Collapsed;
        }
        else if (_ocrResult != null)
        {
            // Always show OCR results when available, even if AI has errors
            OCRLoadingPanel.Visibility = Visibility.Collapsed;
            OCRErrorPanel.Visibility = Visibility.Collapsed;
            OCRResultViewCtrl.Visibility = Visibility.Visible;
            OCREmptyText.Visibility = Visibility.Collapsed;
            OCRResultViewCtrl.SetResult(_ocrResult);
        }
        else if (!string.IsNullOrEmpty(_errorMessage))
        {
            OCRLoadingPanel.Visibility = Visibility.Collapsed;
            OCRErrorPanel.Visibility = Visibility.Visible;
            OCRResultViewCtrl.Visibility = Visibility.Collapsed;
            OCREmptyText.Visibility = Visibility.Collapsed;
            OCRErrorText.Text = _errorMessage;
        }
        else
        {
            OCRLoadingPanel.Visibility = Visibility.Collapsed;
            OCRErrorPanel.Visibility = Visibility.Collapsed;
            OCRResultViewCtrl.Visibility = Visibility.Collapsed;
            OCREmptyText.Visibility = Visibility.Visible;
        }
    }

    private void UpdateDetailState()
    {
        var translation = CurrentTranslation;

        if (translation != null)
        {
            TranslationLoadingPanel.Visibility = Visibility.Collapsed;
            OCRWaitingPanel.Visibility = Visibility.Collapsed;
            TextWaitingPanel.Visibility = Visibility.Collapsed;
            DetailWaitingOCRPanel.Visibility = Visibility.Collapsed;
            DetailErrorPanel.Visibility = Visibility.Collapsed;
            DetailEmptyText.Visibility = Visibility.Collapsed;
            ResultsPanel.Visibility = Visibility.Visible;

            TranslationResultViewCtrl.SetResult(translation, CurrentText);
            AnalysisViewCtrl.SetResult(CurrentAnalysis);
        }
        else if (_ocrResult != null)
        {
            TranslationLoadingPanel.Visibility = _isProcessingAI ? Visibility.Visible : Visibility.Collapsed;
            OCRWaitingPanel.Visibility = _isProcessingAI ? Visibility.Collapsed : Visibility.Visible;
            TextWaitingPanel.Visibility = Visibility.Collapsed;
            DetailWaitingOCRPanel.Visibility = Visibility.Collapsed;
            DetailErrorPanel.Visibility = Visibility.Collapsed;
            DetailEmptyText.Visibility = Visibility.Collapsed;
            ResultsPanel.Visibility = Visibility.Collapsed;

            OCRStatsText.Text = $"OCR 识别完成";
            OCRConfidenceText.Text = $"共识别 {_ocrResult.Blocks.Count} 段文字，平均置信度 {(int)(_ocrResult.AverageConfidence * 100)}%";

            bool hasKey = HasAPIKey;
            StartTranslationBtn.Visibility = hasKey ? Visibility.Visible : Visibility.Collapsed;
            NoAPIKeyHint.Visibility = hasKey ? Visibility.Collapsed : Visibility.Visible;
            ConfigAPIKeyBtn.Visibility = hasKey ? Visibility.Collapsed : Visibility.Visible;
        }
        else if (_inputMode == InputMode.Text && !string.IsNullOrEmpty(_manualText) && !_isProcessingAI)
        {
            TranslationLoadingPanel.Visibility = Visibility.Collapsed;
            OCRWaitingPanel.Visibility = Visibility.Collapsed;
            TextWaitingPanel.Visibility = Visibility.Visible;
            DetailWaitingOCRPanel.Visibility = Visibility.Collapsed;
            DetailErrorPanel.Visibility = Visibility.Collapsed;
            DetailEmptyText.Visibility = Visibility.Collapsed;
            ResultsPanel.Visibility = Visibility.Collapsed;
        }
        else if (_isProcessingOCR)
        {
            TranslationLoadingPanel.Visibility = Visibility.Collapsed;
            OCRWaitingPanel.Visibility = Visibility.Collapsed;
            TextWaitingPanel.Visibility = Visibility.Collapsed;
            DetailWaitingOCRPanel.Visibility = Visibility.Visible;
            DetailErrorPanel.Visibility = Visibility.Collapsed;
            DetailEmptyText.Visibility = Visibility.Collapsed;
            ResultsPanel.Visibility = Visibility.Collapsed;
        }
        else if (!string.IsNullOrEmpty(_errorMessage))
        {
            TranslationLoadingPanel.Visibility = Visibility.Collapsed;
            OCRWaitingPanel.Visibility = Visibility.Collapsed;
            TextWaitingPanel.Visibility = Visibility.Collapsed;
            DetailWaitingOCRPanel.Visibility = Visibility.Collapsed;
            DetailErrorPanel.Visibility = Visibility.Visible;
            DetailEmptyText.Visibility = Visibility.Collapsed;
            ResultsPanel.Visibility = Visibility.Collapsed;
            DetailErrorText.Text = _errorMessage;
        }
        else
        {
            TranslationLoadingPanel.Visibility = Visibility.Collapsed;
            OCRWaitingPanel.Visibility = Visibility.Collapsed;
            TextWaitingPanel.Visibility = Visibility.Collapsed;
            DetailWaitingOCRPanel.Visibility = Visibility.Collapsed;
            DetailErrorPanel.Visibility = Visibility.Collapsed;
            DetailEmptyText.Visibility = Visibility.Visible;
            ResultsPanel.Visibility = Visibility.Collapsed;
        }
    }

    // ── Async helper ──

    private static async Task RunAsync(Func<Task> action)
    {
        try { await action(); }
        catch { /* handled internally */ }
    }
}
