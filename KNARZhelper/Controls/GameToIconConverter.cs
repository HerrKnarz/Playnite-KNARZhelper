using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Data;

namespace KNARZhelper.Controls
{
    public class GameToIconConverter : IValueConverter
    {
        public object Convert(object value, Type TargetType, object parameter, CultureInfo culture)
        {
            var defaultIcon = ResourceProvider.GetResource("DefaultGameIcon") ?? string.Empty;
            Game game = null;

            try
            {
                if (value == null || !(value is Game))
                {
                    return defaultIcon;
                }

                game = (Game)value;

                var platform = game.Platforms.FirstOrDefault(p => p.SpecificationId == "pc_windows") ?? game.Platforms.FirstOrDefault();

                if (!string.IsNullOrEmpty(platform.Icon))
                {
                    var platformIconFileInfo = new FileInfo(API.Instance.Database.GetFullFilePath(platform.Icon));

                    defaultIcon = platformIconFileInfo.Exists ? platformIconFileInfo.FullName : defaultIcon;
                }

                if (string.IsNullOrEmpty(game.Icon))
                {
                    return defaultIcon;
                }

                var fileInfo = new FileInfo(API.Instance.Database.GetFullFilePath(game.Icon));

                return fileInfo.Exists ? fileInfo.FullName : defaultIcon;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error while converting icon from {game?.Name}");

                return defaultIcon;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
