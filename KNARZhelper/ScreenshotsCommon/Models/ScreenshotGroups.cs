using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace KNARZhelper.ScreenshotsCommon.Models
{
    /// <summary>
    /// Collection of screenshot groups.
    /// </summary>
    public partial class ScreenshotGroups : ObservableCollection<ScreenshotGroup>
    {
        private Guid _gameId;

        /// <summary>
        /// Creates a new instance of the ScreenshotGroups class.
        /// </summary>
        public ScreenshotGroups()
        {
        }

        /// <summary>
        /// Creates a new instance of the ScreenshotGroups class and populates it from JSON files.
        /// </summary>
        /// <param name="basePath">
        /// Base path where the JSON files are located. This is the add-on data path containing
        /// folders for each game.
        /// </param>
        /// <param name="gameId">
        /// Unique identifier for the game. This is the name of the sub folder in the base path
        /// </param>
        public ScreenshotGroups(string basePath, Guid gameId)
        {
            _gameId = gameId;
            CreateGroupsFromFiles(basePath, gameId);
        }

        /// <summary>
        /// True if there is more than one screenshot group in the collection. Can be used to
        /// differentiate singular and plural.
        /// </summary>
        [DontSerialize]
        public bool HasMoreThanOneGroup => Count > 1;

        /// <summary>
        /// Specifies whether all screenshots in all groups have been downloaded.
        /// </summary>
        [DontSerialize]
        public bool IsEverythingDownloaded => !this.Any(g => g.Screenshots.Any(s => !s.IsDownloaded || string.IsNullOrEmpty(s.DownloadedThumbnailPath)));

        /// <summary>
        /// Number of screenshots in all groups
        /// </summary>
        [DontSerialize]
        public int ScreenshotCount => Count == 0 ? 0 : this.Sum(g => g.Screenshots.Count);

        /// <summary>
        /// Creates screenshot groups from JSON files located in the specified base path and game ID.
        /// </summary>
        /// <param name="basePath">
        /// Base path where the JSON files are located. This is the add-on data path containing
        /// folders for each game.
        /// </param>
        /// <param name="gameId">
        /// Unique identifier for the game. This is the name of the sub folder in the base path
        /// </param>
        /// <param name="createEmptyGroupOnError"></param>
        public void CreateGroupsFromFiles(string basePath, Guid gameId, bool createEmptyGroupOnError = true)
        {
            _gameId = gameId;

            if (gameId == Guid.Empty)
            {
                if (createEmptyGroupOnError)
                {
                    Add(new ScreenshotGroup(ResourceProvider.GetString("LOCScreenshotUtilitiesMessageNoGameSelected")));
                }

                return;
            }

            var path = ScreenshotHelper.GetDownloadPath(basePath, gameId);

            if (!path.Exists)
            {
                if (createEmptyGroupOnError)
                {
                    Add(new ScreenshotGroup(ResourceProvider.GetString("LOCScreenshotUtilitiesMessageNoScreenshotsFound")));
                }

                return;
            }

            var files = path.EnumerateFiles("*.json", SearchOption.AllDirectories);

            if (!files.Any())
            {
                if (createEmptyGroupOnError)
                {
                    Add(new ScreenshotGroup(ResourceProvider.GetString("LOCScreenshotUtilitiesMessageNoScreenshotsFound")));
                }

                return;
            }

            foreach (var file in files)
            {
                try
                {
                    var group = ScreenshotGroup.CreateFromFile(file);

                    if (group?.Screenshots?.Count > 0)
                    {
                        Add(group);
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Failed to load screenshots from file {file.FullName}");
                }
            }
        }

        public bool DeleteOrphanedFiles()
        {
            var deleted = false;

            if (_gameId == Guid.Empty)
            {
                return deleted;
            }

            try
            {
                var directoryInfo = ScreenshotHelper.GetDownloadPath(gameId: _gameId, createDir: false);

                if (directoryInfo is null
                    || !directoryInfo.Exists
                    || !directoryInfo.FullName.Contains(_gameId.ToString())
                    || !directoryInfo.FullName.Contains(ScreenshotHelper.ScreenshotUtilitiesId.ToString()))
                {
                    return false;
                }

                var filesToKeep = new HashSet<string>();

                foreach (var group in this)
                {
                    filesToKeep.UnionWith(group.Screenshots.Where(s => !string.IsNullOrEmpty(s.DownloadedThumbnailPath)).Select(s => s.DownloadedThumbnailPath));

                    filesToKeep.UnionWith(group.Screenshots.Where(s => !string.IsNullOrEmpty(s.DownloadedPath)).Select(s => s.DownloadedPath));
                }

                var files = directoryInfo.EnumerateFiles("*", SearchOption.AllDirectories).Where(f => f.Extension != ".json").ToList();

                if (!files.Any())
                {
                    return false;
                }

                var result = false;

                foreach (var file in files)
                {
                    if (filesToKeep == null || !filesToKeep.Any(f => file.FullName.Equals(f)))
                    {
                        file.Delete();

                        result = true;
                    }
                }

                if (result)
                {
                    // We wait a bit to ensure the file system has updated
                    Task.Delay(TimeSpan.FromMilliseconds(100));
                }

                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }

            return deleted;
        }

        /// <summary>
        /// Resets the collection by clearing all groups.
        /// </summary>
        public void Reset() => Clear();
    }
}
