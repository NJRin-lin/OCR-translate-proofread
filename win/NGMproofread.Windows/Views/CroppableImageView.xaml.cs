using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace NGMproofread.Windows.Views;

public partial class CroppableImageView : UserControl
{
    public event EventHandler<BitmapSource>? ImageCropped;
    public event EventHandler? RequestNewImage;

    private BitmapSource? _image;
    private double _scale = 1.0;
    private const double MinScale = 1.0;
    private const double MaxScale = 5.0;
    private const double ZoomStep = 0.25;

    private Point _panOffset;
    private Point _lastPanOffset;
    private Point _dragStartPoint;

    private bool _isSelectionMode;
    private Point? _selectionStart;
    private Point? _selectionEnd;
    private Point? _selectionStartOrig;
    private Point? _selectionEndOrig;
    private bool _isDraggingSelection;
    private bool _isMovingSelection;
    private string _dragHandle = ""; // "nw","ne","sw","se","n","s","e","w","move",""

    private const double HandleRadius = 8;
    private const double MinSelectionSize = 10;

    public CroppableImageView()
    {
        InitializeComponent();
    }

    public void SetImage(BitmapSource image)
    {
        _image = image;
        MainImage.Source = image;
        _scale = 1.0;
        _panOffset = new Point(0, 0);
        _selectionStart = null;
        _selectionEnd = null;
        _isSelectionMode = false;
        ApplyTransforms();
        UpdateOverlay();
        UpdateUIStates();
        EmptyStateOverlay.Visibility = Visibility.Collapsed;
    }

    // ── Zoom ──

    private void ZoomInBtn_Click(object sender, RoutedEventArgs e) => ApplyZoom(_scale + ZoomStep);
    private void ZoomOutBtn_Click(object sender, RoutedEventArgs e) => ApplyZoom(_scale - ZoomStep);
    private void ResetZoomBtn_Click(object sender, RoutedEventArgs e) => ApplyZoom(1.0);

    private void ApplyZoom(double newScale)
    {
        newScale = Math.Max(MinScale, Math.Min(MaxScale, newScale));
        if (Math.Abs(newScale - 1.0) < 0.01)
        {
            _panOffset = new Point(0, 0);
        }
        _scale = newScale;
        ApplyTransforms();
        UpdateOverlay();
        UpdateUIStates();
    }

    // ── Pan ──

    private void ImageGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_image == null) return;
        var pos = e.GetPosition(ImageGrid);
        ImageGrid.CaptureMouse();
        _dragStartPoint = pos;

        if (_isSelectionMode)
        {
            var handle = DetectHandle(pos);
            if (!string.IsNullOrEmpty(handle))
            {
                _dragHandle = handle;
                _isDraggingSelection = true;
                return;
            }
            if (IsInsideSelection(pos))
            {
                _dragHandle = "move";
                _isMovingSelection = true;
                _selectionStartOrig = _selectionStart;
                _selectionEndOrig = _selectionEnd;
                return;
            }
            // Start new selection
            _dragHandle = "create";
            _selectionStart = pos;
            _selectionEnd = pos;
            _isDraggingSelection = true;
        }
        else
        {
            _dragHandle = "pan";
            _lastPanOffset = _panOffset;
        }
    }

    private void ImageGrid_MouseMove(object sender, MouseEventArgs e)
    {
        if (_image == null) return;

        var pos = e.GetPosition(ImageGrid);

        // Update cursor
        UpdateCursor(pos);

        if (e.LeftButton != MouseButtonState.Pressed) return;

        var dx = pos.X - _dragStartPoint.X;
        var dy = pos.Y - _dragStartPoint.Y;

        switch (_dragHandle)
        {
            case "pan":
                _panOffset = ClampPan(new Point(_lastPanOffset.X + dx, _lastPanOffset.Y + dy));
                ApplyTransforms();
                break;
            case "create":
                _selectionEnd = pos;
                UpdateOverlay();
                UpdateUIStates();
                break;
            case "move":
                MoveSelection(dx, dy);
                UpdateOverlay();
                break;
            case "nw": ResizeSelection(pos, true, true, false, false); break;
            case "ne": ResizeSelection(pos, false, true, true, false); break;
            case "sw": ResizeSelection(pos, true, false, false, true); break;
            case "se": ResizeSelection(pos, false, false, true, true); break;
            case "n": ResizeSelection(pos, true, false, false, false); break;
            case "s": ResizeSelection(pos, false, false, false, true); break;
            case "w": ResizeSelection(pos, true, false, false, false); break;
            case "e": ResizeSelection(pos, false, false, true, false); break;
        }
    }

    private void ImageGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        ImageGrid.ReleaseMouseCapture();
        _isDraggingSelection = false;
        _isMovingSelection = false;

        // If we were creating a new selection but it's too small, clear it
        if (_dragHandle == "create")
        {
            var rect = GetSelectionRect();
            if (rect == null || rect.Value.Width < MinSelectionSize || rect.Value.Height < MinSelectionSize)
            {
                _selectionStart = null;
                _selectionEnd = null;
            }
            UpdateOverlay();
            UpdateUIStates();
        }

        _dragHandle = "";
        _dragStartPoint = default;
    }

    private void ImageGrid_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (_image == null) return;
        var delta = e.Delta > 0 ? ZoomStep : -ZoomStep;
        ApplyZoom(_scale + delta);
    }

    private void ImageGrid_MouseEnter(object sender, MouseEventArgs e) { }
    private void ImageGrid_MouseLeave(object sender, MouseEventArgs e)
    {
        ImageGrid.Cursor = _isSelectionMode ? Cursors.Cross : Cursors.Arrow;
    }

    // ── Selection actions ──

    private void EnterSelectionBtn_Click(object sender, RoutedEventArgs e)
    {
        _isSelectionMode = true;
        _selectionStart = null;
        _selectionEnd = null;
        UpdateOverlay();
        UpdateUIStates();
    }

    private void CancelSelectionBtn_Click(object sender, RoutedEventArgs e)
    {
        _isSelectionMode = false;
        _selectionStart = null;
        _selectionEnd = null;
        UpdateOverlay();
        UpdateUIStates();
    }

    private void ConfirmSelectionBtn_Click(object sender, RoutedEventArgs e)
    {
        var crop = GetCroppedImage();
        if (crop != null)
        {
            _isSelectionMode = false;
            _selectionStart = null;
            _selectionEnd = null;
            UpdateOverlay();
            UpdateUIStates();
            ImageCropped?.Invoke(this, crop);
        }
    }

    private void FullImageBtn_Click(object sender, RoutedEventArgs e)
    {
        _isSelectionMode = false;
        _selectionStart = null;
        _selectionEnd = null;
        UpdateUIStates();
        if (_image != null)
            ImageCropped?.Invoke(this, _image);
    }

    private void ClearSelectionBtn_Click(object sender, RoutedEventArgs e)
    {
        _isSelectionMode = false;
        _selectionStart = null;
        _selectionEnd = null;
        UpdateOverlay();
        UpdateUIStates();
    }

    private void UploadImageBtn_Click(object sender, RoutedEventArgs e) => RequestNewImage?.Invoke(this, EventArgs.Empty);

    // ── Overlay drawing ──

    private void UpdateOverlay()
    {
        CropCanvas.Children.Clear();
        var rect = GetSelectionRect();
        if (rect == null) return;

        var r = rect.Value;
        var color = _isDraggingSelection || _isMovingSelection
            ? Color.FromRgb(0xFF, 0x95, 0x00)  // Orange
            : Color.FromRgb(0x00, 0x7A, 0xFF); // Blue

        var brush = new SolidColorBrush(color);

        // Semi-transparent overlay: top, bottom, left, right
        var fullW = ImageGrid.ActualWidth;
        var fullH = ImageGrid.ActualHeight;

        // Top
        CropCanvas.Children.Add(new Rectangle
        {
            Width = fullW, Height = Math.Max(0, r.Top),
            Fill = new SolidColorBrush(Color.FromArgb(0x66, 0, 0, 0))
        });
        // Bottom
        CropCanvas.Children.Add(new Rectangle
        {
            Width = fullW, Height = Math.Max(0, fullH - r.Bottom),
            Fill = new SolidColorBrush(Color.FromArgb(0x66, 0, 0, 0)),
            Margin = new Thickness(0, r.Bottom, 0, 0)
        });
        // Left
        CropCanvas.Children.Add(new Rectangle
        {
            Width = Math.Max(0, r.Left), Height = r.Height,
            Fill = new SolidColorBrush(Color.FromArgb(0x66, 0, 0, 0)),
            Margin = new Thickness(0, r.Top, 0, 0)
        });
        // Right
        CropCanvas.Children.Add(new Rectangle
        {
            Width = Math.Max(0, fullW - r.Right), Height = r.Height,
            Fill = new SolidColorBrush(Color.FromArgb(0x66, 0, 0, 0)),
            Margin = new Thickness(r.Right, r.Top, 0, 0)
        });

        // Selection border
        CropCanvas.Children.Add(new Rectangle
        {
            Width = r.Width, Height = r.Height,
            Stroke = brush, StrokeThickness = 2,
            Margin = new Thickness(r.Left, r.Top, 0, 0)
        });

        // Draw handles
        DrawCornerHandle(new Point(r.Left, r.Top), brush);
        DrawCornerHandle(new Point(r.Right, r.Top), brush);
        DrawCornerHandle(new Point(r.Left, r.Bottom), brush);
        DrawCornerHandle(new Point(r.Right, r.Bottom), brush);

        DrawMidHandle(new Point((r.Left + r.Right) / 2, r.Top), brush);
        DrawMidHandle(new Point((r.Left + r.Right) / 2, r.Bottom), brush);
        DrawMidHandle(new Point(r.Left, (r.Top + r.Bottom) / 2), brush);
        DrawMidHandle(new Point(r.Right, (r.Top + r.Bottom) / 2), brush);
    }

    private void DrawCornerHandle(Point pt, SolidColorBrush color)
    {
        var size = 8.0;
        CropCanvas.Children.Add(new Rectangle
        {
            Width = size, Height = size,
            Fill = Brushes.White,
            Stroke = color, StrokeThickness = 1.5,
            RadiusX = 2, RadiusY = 2,
            Margin = new Thickness(pt.X - size / 2, pt.Y - size / 2, 0, 0)
        });
    }

    private void DrawMidHandle(Point pt, SolidColorBrush color)
    {
        var size = 6.0;
        CropCanvas.Children.Add(new Ellipse
        {
            Width = size, Height = size,
            Fill = new SolidColorBrush(Color.FromArgb(0xCC, 0xFF, 0xFF, 0xFF)),
            Stroke = color, StrokeThickness = 1,
            Margin = new Thickness(pt.X - size / 2, pt.Y - size / 2, 0, 0)
        });
    }

    // ── Handle detection ──

    private string DetectHandle(Point pt)
    {
        var rect = GetSelectionRect();
        if (rect == null) return "";

        var r = rect.Value;
        if (Math.Abs(pt.X - r.Left) <= HandleRadius && Math.Abs(pt.Y - r.Top) <= HandleRadius) return "nw";
        if (Math.Abs(pt.X - r.Right) <= HandleRadius && Math.Abs(pt.Y - r.Top) <= HandleRadius) return "ne";
        if (Math.Abs(pt.X - r.Left) <= HandleRadius && Math.Abs(pt.Y - r.Bottom) <= HandleRadius) return "sw";
        if (Math.Abs(pt.X - r.Right) <= HandleRadius && Math.Abs(pt.Y - r.Bottom) <= HandleRadius) return "se";
        if (Math.Abs(pt.X - (r.Left + r.Right) / 2) <= HandleRadius && Math.Abs(pt.Y - r.Top) <= HandleRadius) return "n";
        if (Math.Abs(pt.X - (r.Left + r.Right) / 2) <= HandleRadius && Math.Abs(pt.Y - r.Bottom) <= HandleRadius) return "s";
        if (Math.Abs(pt.Y - (r.Top + r.Bottom) / 2) <= HandleRadius && Math.Abs(pt.X - r.Left) <= HandleRadius) return "w";
        if (Math.Abs(pt.Y - (r.Top + r.Bottom) / 2) <= HandleRadius && Math.Abs(pt.X - r.Right) <= HandleRadius) return "e";
        return "";
    }

    private bool IsInsideSelection(Point pt)
    {
        var rect = GetSelectionRect();
        return rect != null && rect.Value.Contains(pt);
    }

    private void UpdateCursor(Point pt)
    {
        if (_isDraggingSelection || _isMovingSelection) return;

        if (_isSelectionMode)
        {
            var handle = DetectHandle(pt);
            ImageGrid.Cursor = handle switch
            {
                "nw" or "se" => Cursors.SizeNWSE,
                "ne" or "sw" => Cursors.SizeNESW,
                "n" or "s" => Cursors.SizeNS,
                "e" or "w" => Cursors.SizeWE,
                _ => IsInsideSelection(pt) ? Cursors.SizeAll : Cursors.Cross
            };
        }
        else
        {
            ImageGrid.Cursor = Cursors.SizeAll;
        }
    }

    // ── Selection manipulation ──

    private Rect? GetSelectionRect()
    {
        if (_selectionStart == null || _selectionEnd == null) return null;
        var s = _selectionStart.Value;
        var e = _selectionEnd.Value;
        return new Rect(
            Math.Min(s.X, e.X), Math.Min(s.Y, e.Y),
            Math.Abs(e.X - s.X), Math.Abs(e.Y - s.Y));
    }

    private void MoveSelection(double dx, double dy)
    {
        if (_selectionStartOrig == null || _selectionEndOrig == null) return;
        _selectionStart = new Point(_selectionStartOrig.Value.X + dx, _selectionStartOrig.Value.Y + dy);
        _selectionEnd = new Point(_selectionEndOrig.Value.X + dx, _selectionEndOrig.Value.Y + dy);
    }

    private void ResizeSelection(Point pt, bool setLeft, bool setTop, bool setRight, bool setBottom)
    {
        if (_selectionStart == null || _selectionEnd == null) return;
        var s = _selectionStart.Value;
        var e = _selectionEnd.Value;

        // Clamp to image bounds
        var imgBounds = new Rect(0, 0, ImageGrid.ActualWidth, ImageGrid.ActualHeight);
        pt.X = Math.Max(imgBounds.Left, Math.Min(pt.X, imgBounds.Right));
        pt.Y = Math.Max(imgBounds.Top, Math.Min(pt.Y, imgBounds.Bottom));

        var left = setLeft ? pt.X : Math.Min(s.X, e.X);
        var top = setTop ? pt.Y : Math.Min(s.Y, e.Y);
        var right = setRight ? pt.X : Math.Max(s.X, e.X);
        var bottom = setBottom ? pt.Y : Math.Max(s.Y, e.Y);

        if (Math.Abs(right - left) >= MinSelectionSize && Math.Abs(bottom - top) >= MinSelectionSize)
        {
            _selectionStart = new Point(left, top);
            _selectionEnd = new Point(right, bottom);
        }

        UpdateOverlay();
    }

    // ── Crop ──

    private BitmapSource? GetCroppedImage()
    {
        var rect = GetSelectionRect();
        if (rect == null || _image == null) return null;

        var r = rect.Value;
        var imgWidth = _image.PixelWidth;
        var imgHeight = _image.PixelHeight;

        // Calculate the displayed image bounds within the Grid
        var displaySize = GetDisplayedImageSize();
        var offsetX = (ImageGrid.ActualWidth - displaySize.Width) / 2;
        var offsetY = (ImageGrid.ActualHeight - displaySize.Height) / 2;

        // Map selection coords to pixel coords (account for pan offset)
        double px = (r.X - offsetX - _panOffset.X) / displaySize.Width * imgWidth;
        double py = (r.Y - offsetY - _panOffset.Y) / displaySize.Height * imgHeight;
        double pw = r.Width / displaySize.Width * imgWidth;
        double ph = r.Height / displaySize.Height * imgHeight;

        px = Math.Max(0, Math.Floor(px));
        py = Math.Max(0, Math.Floor(py));
        pw = Math.Min(Math.Floor(pw), imgWidth - px);
        ph = Math.Min(Math.Floor(ph), imgHeight - py);

        if (pw <= 0 || ph <= 0) return null;

        try
        {
            return new CroppedBitmap(_image, new Int32Rect((int)px, (int)py, (int)pw, (int)ph));
        }
        catch
        {
            return null;
        }
    }

    private Size GetDisplayedImageSize()
    {
        if (_image == null) return new Size();

        var imgAspect = (double)_image.PixelWidth / _image.PixelHeight;
        var gridW = ImageGrid.ActualWidth * _scale;
        var gridH = ImageGrid.ActualHeight * _scale;

        double w, h;
        if (imgAspect > gridW / gridH)
        {
            w = gridW;
            h = gridW / imgAspect;
        }
        else
        {
            h = gridH;
            w = gridH * imgAspect;
        }

        return new Size(w, h);
    }

    private Point ClampPan(Point offset) => offset; // Simplified

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

        var hasSelection = GetSelectionRect() != null;

        if (_isDraggingSelection || _isMovingSelection)
            ToolbarHintText.Text = "调整中…";
        else if (hasSelection)
            ToolbarHintText.Text = "拖手柄调整 · 拖内部移动";
        else if (_isSelectionMode)
            ToolbarHintText.Text = "拖拽框选";
        else
            ToolbarHintText.Text = "滑动平移 · 滚轮缩放";

        ClearSelectionBtn.Visibility = hasSelection ? Visibility.Visible : Visibility.Collapsed;

        if (hasSelection)
        {
            EnterSelectionBtn.Visibility = Visibility.Collapsed;
            CancelSelectionBtn.Visibility = Visibility.Collapsed;
            ConfirmSelectionBtn.Visibility = Visibility.Visible;
        }
        else if (_isSelectionMode)
        {
            EnterSelectionBtn.Visibility = Visibility.Collapsed;
            CancelSelectionBtn.Visibility = Visibility.Visible;
            ConfirmSelectionBtn.Visibility = Visibility.Collapsed;
        }
        else
        {
            EnterSelectionBtn.Visibility = Visibility.Visible;
            CancelSelectionBtn.Visibility = Visibility.Collapsed;
            ConfirmSelectionBtn.Visibility = Visibility.Collapsed;
        }
    }
}
