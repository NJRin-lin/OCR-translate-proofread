using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace NGMproofread.Windows.Views;

public partial class ZoomableImageView : UserControl
{
    private double _scale = 1.0;
    private const double MinScale = 1.0;
    private const double MaxScale = 5.0;
    private const double ZoomStep = 0.25;

    private Point _panOffset;
    private Point _lastPanOffset;
    private Point _dragStart;
    private bool _isPanning;

    public ZoomableImageView()
    {
        InitializeComponent();
    }

    public void SetImage(BitmapSource image)
    {
        MainImage.Source = image;
        _scale = 1.0;
        _panOffset = new Point(0, 0);
        ApplyTransforms();
        UpdateUIStates();
    }

    private void ZoomInBtn_Click(object sender, RoutedEventArgs e) => ApplyZoom(_scale + ZoomStep);
    private void ZoomOutBtn_Click(object sender, RoutedEventArgs e) => ApplyZoom(_scale - ZoomStep);
    private void ResetZoomBtn_Click(object sender, RoutedEventArgs e) => ApplyZoom(1.0);

    private void ApplyZoom(double newScale)
    {
        newScale = Math.Max(MinScale, Math.Min(MaxScale, newScale));
        if (Math.Abs(newScale - 1.0) < 0.01)
            _panOffset = new Point(0, 0);
        _scale = newScale;
        ApplyTransforms();
        UpdateUIStates();
    }

    private void ImageGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStart = e.GetPosition(this);
        _lastPanOffset = _panOffset;
        _isPanning = true;
        Mouse.Capture((UIElement)sender);
    }

    private void ImageGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isPanning = false;
        Mouse.Capture(null);
    }

    private void ImageGrid_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isPanning) return;
        var pos = e.GetPosition(this);
        _panOffset = new Point(
            _lastPanOffset.X + pos.X - _dragStart.X,
            _lastPanOffset.Y + pos.Y - _dragStart.Y);
        ApplyTransforms();
    }

    private void ImageGrid_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        var delta = e.Delta > 0 ? ZoomStep : -ZoomStep;
        ApplyZoom(_scale + delta);
    }

    private void ApplyTransforms()
    {
        ImageScale.ScaleX = _scale;
        ImageScale.ScaleY = _scale;
        ImageTranslate.X = _panOffset.X;
        ImageTranslate.Y = _panOffset.Y;
    }

    private void UpdateUIStates()
    {
        ZoomPercentText.Text = $"{(int)(_scale * 100)}%";
        ResetZoomBtn.Visibility = Math.Abs(_scale - 1.0) > 0.01 ? Visibility.Visible : Visibility.Collapsed;
    }
}
