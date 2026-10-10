using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;

namespace DiscordNative.App.Services;

public sealed class StringToImageSourceConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string url && !string.IsNullOrEmpty(url))
        {
            var bitmap = new BitmapImage(new Uri(url));
            // Disable automatic GIF animation playback by default so avatars/guild icons don't play constantly
            bitmap.AutoPlay = false;
            return bitmap;
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
