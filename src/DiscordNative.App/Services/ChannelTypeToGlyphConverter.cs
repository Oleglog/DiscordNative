using Microsoft.UI.Xaml.Data;
using DiscordNative.Protocol.Models;

namespace DiscordNative.App.Services;

public sealed class ChannelTypeToGlyphConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is DiscordChannelType type)
        {
            return type switch
            {
                DiscordChannelType.GuildVoice or DiscordChannelType.GuildStageVoice => "\uE767",
                _ => "\uE8BD",
            };
        }
        return "\uE8BD";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
