using Playnite.SDK.Models;

namespace KNARZhelper.GamesCommon
{
    public class GameEx
    {
        public Game Game { get; set; }

        public string Platforms { get; set; }

        public string RealSortingName => string.IsNullOrEmpty(Game.SortingName) ? Game.Name : Game.SortingName;
    }
}
