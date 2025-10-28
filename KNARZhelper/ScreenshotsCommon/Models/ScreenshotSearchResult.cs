using Playnite.SDK;

namespace KNARZhelper.ScreenshotsCommon.Models
{
    /// <summary>
    /// Model for the search results of a screenshot search
    /// </summary>
    public class ScreenshotSearchResult : GenericItemOption
    {
        /// <summary>
        /// Identifier for the provider addon to identify the right game in the screenshot source
        /// (e.g. Steam-ID, URL to the API endpoint or similar)
        /// </summary>
        public string Identifier;
    }
}
