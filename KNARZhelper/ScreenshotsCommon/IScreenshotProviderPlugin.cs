using Playnite.SDK.Models;
using System.Threading.Tasks;

namespace KNARZhelper.ScreenshotsCommon
{
    /// <summary>
    /// Interface for Screenshot Utilities Provider addons for methods and properties used by the main Screenshot Utilities addon.
    /// </summary>
    public interface IScreenshotProviderPlugin
    {
        /// <summary>
        /// Asynchronous method to clean up screenshots for a given game. This gets called when a game is 
        /// loaded in Playnite. Can be used to delete orphaned json files of make adjustments in them. 
        /// Usually it's not needed to delete orphaned screenshot or thumbnail files as those are handled
        /// by the main Screenshot Utilities addon.
        /// </summary>
        /// <param name="game">Game to clean up</param>
        /// <returns>True if something was cleaned up</returns>
        Task<bool> CleanUpAsync(Game game);

        /// <summary>
        /// Asynchronous method to add screenshots to a game. This method gets called when the automatic 
        /// screenshot refresh is running. The addon needs to look for an already existing json file and 
        /// refresh the screenshots in it or create a new one for the game if none exists already. When no
        /// screenshots were found, no file needs to be created.
        /// </summary>
        /// <param name="game">Game to find screenshots for</param>
        /// <param name="daysSinceLastUpdate">days since the last refresh. The method should cancel when
        /// the property "LastUpdate" of an existing file is newer than this.</param>
        /// <param name="forceUpdate">When true the screenshots should always be refreshed, ignoring 
        /// "daysSinceLastUpdate"</param>
        /// <returns>True if new screenshots were added.</returns>
        Task<bool> GetScreenshotsAsync(Game game, int daysSinceLastUpdate, bool forceUpdate);

        /// <summary>
        /// Asynchronous method to add screenshots to a game based on a search result. This gets called after
        /// GetScreenshotSearchResult was used to get a list of search results and the user selected one of
        /// those results. It should replace existing screenshots if those already existing are from another
        /// game.
        /// </summary>
        /// <param name="game">Game to find screenshots for</param>
        /// <param name="gameIdentifier">Game identifier of the search result the user selected. This should
        /// have the information needed to unambiguously get screenshots for the game.</param>
        /// <returns>True if new screenshots were added.</returns>
        Task<bool> GetScreenshotsManualAsync(Game game, string gameIdentifier);

        /// <summary>
        /// Synchronous method to provide search results for a given search term or game. This is called when
        /// a user clicks on the search screenshots menu entry to manually search for screenshots from this
        /// provider
        /// </summary>
        /// <param name="game">Game to find screenshots for</param>
        /// <param name="searchTerm">Term to search for. When empty the addon needs to use the appropriate 
        /// info from the game (usually the name of the game)</param>
        /// <returns>json with a list of search results</returns>
        string GetScreenshotSearchResult(Game game, string searchTerm);

        /// <summary>
        /// Returns true if the provider addon supports finding images automatically with the information
        /// from the game alone.
        /// </summary>
        bool SupportsAutomaticScreenshots { get; set; }

        /// <summary>
        /// Returns true if the provider addon supports finding images through searching. Needs to return
        /// search results via GetScreenshotSearchResult and add images via GetScreenshotsManualAsync
        /// </summary>
        bool SupportsScreenshotSearch { get; set; }
    }
}
