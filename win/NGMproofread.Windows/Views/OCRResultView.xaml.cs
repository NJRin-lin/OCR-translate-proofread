using System.ComponentModel;
using System.Linq;
using System.Speech.Synthesis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NGMproofread.Windows.Models;

namespace NGMproofread.Windows.Views;

public partial class OCRResultView : UserControl
{
    private OCRResult? _result;
    private bool _showContinuous;
    private SpeechSynthesizer? _synth;
    private bool _isSpeaking;

    public OCRResultView()
    {
        InitializeComponent();
    }

    public void SetResult(OCRResult result)
    {
        _result = result;
        _showContinuous = false;
        RenderResult();
    }

    private void ViewToggleBtn_Click(object sender, RoutedEventArgs e)
    {
        _showContinuous = !_showContinuous;
        RenderResult();
    }

    private void TTSButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isSpeaking)
        {
            StopTTS();
        }
        else
        {
            StartTTS();
        }
    }

    private void StartTTS()
    {
        if (_synth == null)
        {
            _synth = new SpeechSynthesizer();
            var jpVoice = _synth.GetInstalledVoices()
                .FirstOrDefault(v => v.VoiceInfo.Culture.Name.StartsWith("ja"));
            if (jpVoice != null)
                _synth.SelectVoice(jpVoice.VoiceInfo.Name);

            _synth.SpeakCompleted += (_, _) =>
            {
                _isSpeaking = false;
                Dispatcher.Invoke(() => TTSButton.Content = "▶ 朗读");
            };
        }

        var text = _result?.FullText ?? "";
        _isSpeaking = true;
        TTSButton.Content = "⏹ 停止";

        var ssml = $"<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='ja-JP'>{System.Net.WebUtility.HtmlEncode(text)}</speak>";
        _synth.SpeakSsmlAsync(ssml);
    }

    private void StopTTS()
    {
        _synth?.SpeakAsyncCancelAll();
        _isSpeaking = false;
        TTSButton.Content = "▶ 朗读";
    }

    private void RenderResult()
    {
        if (_result == null) return;

        ViewToggleBtn.Content = _showContinuous ? "分段" : "连贯";
        TTSButton.Visibility = _showContinuous ? Visibility.Visible : Visibility.Collapsed;

        // Confidence badge
        var avgConf = _result.AverageConfidence;
        var pct = (int)(avgConf * 100);
        var color = avgConf >= 0.8f ? "#34C759" : avgConf >= 0.6f ? "#FF9500" : "#FF3B30";
        ConfidenceBadgeText.Text = $"平均置信度 {pct}%";
        ConfidenceBadgeText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        ConfidenceBadge.Background = new SolidColorBrush(Color.FromArgb(0x1A,
            ((Color)ColorConverter.ConvertFromString(color)).R,
            ((Color)ColorConverter.ConvertFromString(color)).G,
            ((Color)ColorConverter.ConvertFromString(color)).B));

        if (_showContinuous)
        {
            ContinuousTextView.Visibility = Visibility.Visible;
            SegmentedList.Visibility = Visibility.Collapsed;
            ContinuousTextView.Text = _result.FullText;
        }
        else
        {
            ContinuousTextView.Visibility = Visibility.Collapsed;
            SegmentedList.Visibility = Visibility.Visible;
            SegmentedList.ItemsSource = _result.Blocks.Select(b => new OCRBlockDisplay
            {
                Text = b.Text,
                ConfidenceDisplay = $"{b.Confidence * 100:F0}%",
                ConfidenceBrush = GetConfidenceBrush(b.Confidence),
                IsLowConfidence = b.Confidence < 0.7f
            }).ToList();
        }

        // Warning for low confidence
        LowConfidenceWarning.Visibility =
            _result.LowConfidenceBlocks.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private static SolidColorBrush GetConfidenceBrush(float conf)
    {
        var color = conf >= 0.8f ? "#34C759" : conf >= 0.6f ? "#FF9500" : "#FF3B30";
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
    }
}

public class OCRBlockDisplay
{
    public string Text { get; set; } = "";
    public string ConfidenceDisplay { get; set; } = "";
    public SolidColorBrush ConfidenceBrush { get; set; } = Brushes.Gray;
    public bool IsLowConfidence { get; set; }
}
