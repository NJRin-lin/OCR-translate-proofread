using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace NGMproofread.Windows.Utilities;

public class ImageLoader
{
    public async Task<BitmapSource?> LoadFromFileAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择图片",
            Filter = "图片文件|*.png;*.jpg;*.jpeg;*.bmp;*.tiff|所有文件|*.*",
            Multiselect = false
        };

        var result = dialog.ShowDialog();
        if (result != true) return null;

        return await Task.Run(() => LoadBitmapFromFile(dialog.FileName));
    }

    private static BitmapSource? LoadBitmapFromFile(string path)
    {
        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(path);
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }
}
