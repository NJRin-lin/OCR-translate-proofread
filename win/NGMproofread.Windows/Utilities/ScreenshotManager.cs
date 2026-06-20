using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace NGMproofread.Windows.Utilities;

public class ScreenshotManager
{
    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;

    public async Task<BitmapSource?> CaptureAsync()
    {
        return await Task.Run(() =>
        {
            var width = GetSystemMetrics(SM_CXSCREEN);
            var height = GetSystemMetrics(SM_CYSCREEN);

            using var bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(0, 0, 0, 0, new System.Drawing.Size(width, height));

            return ConvertBitmapToBitmapSource(bitmap);
        });
    }

    public async Task<BitmapSource?> CaptureFromClipboardAsync()
    {
        return await Task.Run(() =>
        {
            if (!System.Windows.Clipboard.ContainsImage()) return null;
            var bitmapSource = System.Windows.Clipboard.GetImage();
            return bitmapSource;
        });
    }

    private static BitmapSource ConvertBitmapToBitmapSource(Bitmap bitmap)
    {
        var hBitmap = bitmap.GetHbitmap();
        try
        {
            var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap, IntPtr.Zero, Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            bitmapSource.Freeze();
            return bitmapSource;
        }
        finally
        {
            NativeMethods.DeleteObject(hBitmap);
        }
    }
}

internal static class NativeMethods
{
    [System.Runtime.InteropServices.DllImport("gdi32.dll")]
    public static extern bool DeleteObject(IntPtr hObject);
}
