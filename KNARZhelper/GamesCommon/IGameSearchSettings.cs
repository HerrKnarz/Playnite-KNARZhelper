namespace KNARZhelper.GamesCommon
{
    public interface IGameSearchSettings
    {
        bool GameGridShowCompletionStatus { get; set; }
        bool GameGridShowHidden { get; set; }
        bool GameGridShowPlatform { get; set; }
        bool GameGridShowReleaseYear { get; set; }
        int GameSearchWindowHeight { get; set; }
        int GameSearchWindowWidth { get; set; }
    }
}
