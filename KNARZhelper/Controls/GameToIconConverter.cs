using KNARZhelper.GamesCommon;
using Playnite.SDK.Models;
using System;
using System.Globalization;
using System.Windows.Data;

namespace KNARZhelper.Controls
{
    public class GameToIconConverter : IValueConverter
    {
        public object Convert(object value, Type TargetType, object parameter, CultureInfo culture)
        {
            Game game = null;

            try
            {
                if (value == null || !(value is Game))
                {
                    return GameEx.DefaultIcon;
                }

                game = (Game)value;

                return GameEx.GetGameIconPath(game);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error while converting icon from {game?.Name}");

                return GameEx.DefaultIcon;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
