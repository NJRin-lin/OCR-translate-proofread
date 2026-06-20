using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using NGMproofread.Windows.Models;

namespace NGMproofread.Windows.Views;

public partial class AnalysisView : UserControl
{
    private AnalysisResult? _result;

    public AnalysisView()
    {
        InitializeComponent();
    }

    public void SetResult(AnalysisResult? result)
    {
        _result = result;
        RenderResult();
    }

    private void RenderResult()
    {
        SentenceCards.Items.Clear();

        if (_result == null)
        {
            ModeBadge.Visibility = Visibility.Collapsed;
            return;
        }

        ModeBadge.Visibility = Visibility.Visible;
        ModeBadgeText.Text = _result.Mode.DisplayName();

        foreach (var sentence in _result.Sentences)
        {
            var card = CreateSentenceCard(sentence);
            SentenceCards.Items.Add(card);
        }

        // Overall notes
        if (!string.IsNullOrEmpty(_result.OverallNotes))
        {
            var notesPanel = new Border
            {
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(Color.FromArgb(0x0F, 0xFF, 0xCC, 0x00)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(0x33, 0xFF, 0xCC, 0x00)),
                BorderThickness = new Thickness(0.5),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 12, 0, 0)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var icon = new TextBlock
            {
                Text = "💡", FontSize = 15,
                Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0xCC, 0x00)),
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 0, 6, 0)
            };
            Grid.SetColumn(icon, 0);
            grid.Children.Add(icon);

            var tb = new TextBlock
            {
                Text = _result.OverallNotes,
                FontSize = 15,
                Foreground = new SolidColorBrush(Colors.Gray),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetColumn(tb, 1);
            grid.Children.Add(tb);
            notesPanel.Child = grid;

            SentenceCards.Items.Add(notesPanel);
        }
    }

    private FrameworkElement CreateSentenceCard(SentenceAnalysis sentence)
    {
        var stack = new StackPanel();

        // Original sentence (read-only TextBox for copy support)
        var origBlock = new TextBox
        {
            Text = sentence.OriginalSentence,
            FontSize = 14, FontWeight = FontWeights.Bold,
            FontFamily = new FontFamily("Meiryo, MS Gothic, Yu Gothic"),
            TextWrapping = TextWrapping.Wrap,
            IsReadOnly = true,
            BorderThickness = new Thickness(0),
            Background = Brushes.Transparent,
            Margin = new Thickness(14, 10, 14, 0)
        };
        stack.Children.Add(origBlock);

        // Components tree
        if (sentence.Components.Count > 0)
        {
            stack.Children.Add(new Separator { Margin = new Thickness(0, 8, 0, 0) });

            var compHeader = new TextBlock
            {
                Text = "句子成分", FontSize = 15,
                Foreground = new SolidColorBrush(Colors.Gray),
                Margin = new Thickness(14, 8, 14, 6)
            };
            stack.Children.Add(compHeader);

            var treeLines = RenderTree(sentence.Components, "");
            var treeText = string.Join("\n", treeLines);
            var treeBox = new TextBlock
            {
                Text = treeText,
                FontSize = 15,
                FontFamily = new FontFamily("Consolas, Cascadia Code, monospace"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(14, 0, 14, 8),
                Padding = new Thickness(10),
                Background = new SolidColorBrush(Color.FromArgb(0x80, 0x80, 0x80, 0x80))
            };
            stack.Children.Add(treeBox);
        }

        // Grammar points
        if (sentence.GrammarPoints.Count > 0)
        {
            stack.Children.Add(new Separator { Margin = new Thickness(0, 4, 0, 0) });

            var gramHeader = new TextBlock
            {
                Text = "语法要点", FontSize = 15,
                Foreground = new SolidColorBrush(Colors.Gray),
                Margin = new Thickness(14, 8, 14, 4)
            };
            stack.Children.Add(gramHeader);

            foreach (var point in sentence.GrammarPoints)
            {
                var gpStack = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(14, 0, 14, 2) };
                gpStack.Children.Add(new TextBlock
                {
                    Text = "·", FontSize = 15,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xAF, 0x52, 0xDE))
                });
                gpStack.Children.Add(new TextBlock
                {
                    Text = point, FontSize = 15,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(6, 0, 0, 0)
                });
                stack.Children.Add(gpStack);
            }
        }

        // Vocabulary
        if (sentence.Vocabulary.Count > 0)
        {
            var filtered = FilterVocab(sentence.Vocabulary);
            if (filtered.Count > 0)
            {
                stack.Children.Add(new Separator { Margin = new Thickness(0, 4, 0, 0) });

                var vocabHeader = new TextBlock
                {
                    Text = "词汇注解", FontSize = 15,
                    Foreground = new SolidColorBrush(Colors.Gray),
                    Margin = new Thickness(14, 8, 14, 4)
                };
                stack.Children.Add(vocabHeader);

                foreach (var word in filtered)
                {
                    var wordStack = new StackPanel { Margin = new Thickness(14, 0, 14, 8) };

                    var headStack = new StackPanel { Orientation = Orientation.Horizontal };
                    headStack.Children.Add(new TextBlock
                    {
                        Text = word.Word, FontSize = 15, FontWeight = FontWeights.Bold,
                        FontFamily = new FontFamily("Yu Mincho, MS Mincho, serif")
                    });
                    headStack.Children.Add(new TextBlock
                    {
                        Text = $" ({word.Reading})", FontSize = 15,
                        Foreground = new SolidColorBrush(Colors.Gray),
                        VerticalAlignment = VerticalAlignment.Center
                    });
                    headStack.Children.Add(new TextBlock
                    {
                        Text = $" {word.Meaning}", FontSize = 15,
                        VerticalAlignment = VerticalAlignment.Center
                    });

                    // Tag capsules
                    if (!string.IsNullOrEmpty(word.PartOfSpeech))
                    {
                        var posTag = new Border
                        {
                            CornerRadius = new CornerRadius(8),
                            Background = new SolidColorBrush(Color.FromArgb(0x1A, 0x00, 0x7A, 0xFF)),
                            Padding = new Thickness(5, 1, 5, 1),
                            Margin = new Thickness(4, 0, 0, 0)
                        };
                        posTag.Child = new TextBlock
                        {
                            Text = word.PartOfSpeech, FontSize = 12,
                            Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0x7A, 0xFF))
                        };
                        headStack.Children.Add(posTag);
                    }

                    if (!string.IsNullOrEmpty(word.JlptLevel))
                    {
                        var levelTag = new Border
                        {
                            CornerRadius = new CornerRadius(8),
                            Background = new SolidColorBrush(Color.FromArgb(0x1A, 0xFF, 0x95, 0x00)),
                            Padding = new Thickness(5, 1, 5, 1),
                            Margin = new Thickness(4, 0, 0, 0)
                        };
                        levelTag.Child = new TextBlock
                        {
                            Text = word.JlptLevel, FontSize = 12,
                            Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x95, 0x00))
                        };
                        headStack.Children.Add(levelTag);
                    }

                    wordStack.Children.Add(headStack);

                    if (!string.IsNullOrEmpty(word.Notes))
                    {
                        wordStack.Children.Add(new TextBlock
                        {
                            Text = word.Notes, FontSize = 15,
                            Foreground = new SolidColorBrush(Colors.Gray),
                            Margin = new Thickness(0, 2, 0, 0)
                        });
                    }

                    stack.Children.Add(wordStack);
                }
            }
        }

        // Wrap in card
        var card = new Border
        {
            CornerRadius = new CornerRadius(8),
            Background = SystemColors.WindowBrush,
            BorderBrush = new SolidColorBrush(Color.FromArgb(0x33, 0x80, 0x80, 0x80)),
            BorderThickness = new Thickness(0.5),
            Margin = new Thickness(0, 0, 0, 8),
            Padding = new Thickness(0, 0, 0, 10),
            Child = stack
        };

        return card;
    }

    private List<string> RenderTree(List<SentenceComponent> components, string prefix)
    {
        var lines = new List<string>();
        for (int i = 0; i < components.Count; i++)
        {
            var comp = components[i];
            bool isLast = i == components.Count - 1;
            var branch = isLast ? "└─ " : "├─ ";
            var label = comp.Label;
            var text = comp.Text;
            var expl = !string.IsNullOrEmpty(comp.Explanation) ? $"（{comp.Explanation}）" : "";

            var line = prefix + branch + label + "：" + text + expl;
            lines.Add(line);

            if (comp.Children.Count > 0)
            {
                var childPrefix = prefix + (isLast ? "   " : "│  ");
                lines.AddRange(RenderTree(comp.Children, childPrefix));
            }
        }
        return lines;
    }

    private List<VocabularyAnnotation> FilterVocab(List<VocabularyAnnotation> words)
    {
        if (_result?.Mode != AnalysisMode.Study) return words;

        return words.Where(v =>
        {
            if (v.JlptLevel == null) return true;
            if (int.TryParse(v.JlptLevel.Replace("N", ""), out var n))
                return n <= 2;
            return true;
        }).ToList();
    }
}
