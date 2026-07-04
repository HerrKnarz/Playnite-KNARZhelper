using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.IO;
using System.Linq;

namespace KNARZhelper.GamesCommon
{
    public class GameEx
    {
        public GameEx(Game game = null)
        {
            Game = game;
        }

        public static string DefaultCover => ResourceProvider.GetResource("DefaultGameCover")?.ToString() ?? string.Empty;
        public static string DefaultIcon => ResourceProvider.GetResource("DefaultGameIcon")?.ToString() ?? string.Empty;
        public Game Game { get; set; } = new Game();

        public string Platforms { get; set; }

        public string RealSortingName => string.IsNullOrEmpty(Game.SortingName) ? Game.Name : Game.SortingName;

        public static string GetGameCoverPath(Game game)
        {
            try
            {
                if (game is null)
                {
                    return DefaultCover;
                }

                var platform = game.Platforms?.FirstOrDefault(p => p.SpecificationId == "pc_windows") ?? game.Platforms?.FirstOrDefault();
                var defaultCover = DefaultCover;

                if (!string.IsNullOrEmpty(platform?.Cover))
                {
                    var platformCoverFileInfo = new FileInfo(API.Instance.Database.GetFullFilePath(platform.Cover));
                    defaultCover = platformCoverFileInfo.Exists ? platformCoverFileInfo.FullName : defaultCover;
                }

                if (string.IsNullOrEmpty(game.CoverImage))
                {
                    return defaultCover;
                }

                var fileInfo = new FileInfo(API.Instance.Database.GetFullFilePath(game.CoverImage));
                return fileInfo.Exists ? fileInfo.FullName : defaultCover;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error while getting cover from {game?.Name}");

                return DefaultCover;
            }
        }

        public static string GetGameIconPath(Game game)
        {
            try
            {
                if (game is null)
                {
                    return DefaultIcon;
                }

                var platform = game.Platforms?.FirstOrDefault(p => p.SpecificationId == "pc_windows") ?? game.Platforms?.FirstOrDefault();
                var defaultIcon = DefaultIcon;

                if (!string.IsNullOrEmpty(platform?.Icon))
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
                Log.Error(ex, $"Error while getting icon from {game?.Name}");

                return DefaultIcon;
            }
        }
    }
}
