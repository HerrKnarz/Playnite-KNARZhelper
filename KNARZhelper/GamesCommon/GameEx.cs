using Playnite.SDK.Models;

namespace KNARZhelper.GamesCommon
{
    public class GameEx
    {
        public GameEx(Game game = null)
        {
            Game = game;
        }

        public Game Game { get; set; } = new Game();

        public string Platforms { get; set; }

        public string RealSortingName => string.IsNullOrEmpty(Game.SortingName) ? Game.Name : Game.SortingName;
    }
}
