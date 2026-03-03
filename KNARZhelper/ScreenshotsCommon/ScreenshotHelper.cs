using KNARZhelper.ScreenshotsCommon.Models;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace KNARZhelper.ScreenshotsCommon
{
    internal static class ScreenshotHelper
    {
        internal static Guid ScreenshotUtilitiesId = Guid.Parse("485d682f-73e9-4d54-b16f-b8dd49e88f90");

        /// <summary>
        /// Checks if Screenshot Utilities plugin is installed.
        /// </summary>
        internal static bool IsScreenshotUtilitiesInstalled => API.Instance.Addons.Plugins.Exists(p => p.Id == ScreenshotUtilitiesId);

        /// <summary>
        /// Gets the download path based on the base path, game ID, and provider ID.
        /// </summary>
        /// <param name="basePath">The base path for the download location.</param>
        /// <param name="gameId">The ID of the game.</param>
        /// <param name="providerId">The ID of the provider.</param>
        /// <returns>The directory info for the download path.</returns>
        public static DirectoryInfo GetDownloadPath(string basePath = null, Guid gameId = default, Guid providerId = default, bool createDir = true)
        {
            try
            {
                if (string.IsNullOrEmpty(basePath))
                {
                    basePath = API.Instance.Addons.Plugins.Find(p => p.Id == ScreenshotUtilitiesId).GetPluginUserDataPath();
                }

                if (string.IsNullOrEmpty(basePath) || !Directory.Exists(basePath))
                {
                    return null;
                }

                var path = Path.Combine(basePath,
                    gameId == default ? string.Empty : gameId.ToString(),
                    providerId == default ? string.Empty : providerId.ToString());

                var directoryInfo = new DirectoryInfo(path);

                if (createDir && !directoryInfo.Exists)
                {
                    directoryInfo.Create();
                    Task.Delay(TimeSpan.FromMilliseconds(100));
                    directoryInfo.Refresh();
                }

                return directoryInfo;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to create download path.");

                return null;
            }
        }

        /// <summary>
        /// Deletes orphaned JSON files in the specified game's provider directory,
        /// </summary>
        /// <param name="gameId">Id of the game</param>
        /// <param name="providerId">Id of the provider</param>
        /// <param name="filesToKeep">List of file IDs to keep</param>
        /// <returns>True, if files were deleted</returns>
        internal static async Task<bool> DeleteOrphanedJsonFiles(Guid gameId, Guid providerId, List<Guid> filesToKeep = null)
        {
            if (!IsScreenshotUtilitiesInstalled)
            {
                return false;
            }

            var directoryInfo = GetDownloadPath(gameId: gameId, providerId: providerId);

            if (directoryInfo is null || !directoryInfo.Exists)
            {
                return false;
            }

            if (filesToKeep is null)
            {
                filesToKeep = new List<Guid>() { providerId };
            }

            var jsonFiles = directoryInfo.GetFiles("*.json");

            var result = false;

            foreach (var jsonFile in jsonFiles)
            {
                if (filesToKeep == null || !filesToKeep.Any(file => jsonFile.Name.Contains(file.ToString())))
                {
                    jsonFile.Delete();

                    result = true;
                }
            }

            if (result)
            {
                // We wait a bit to ensure the file system has updated
                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }

            return result;
        }

        /// <summary>
        /// Generates the file name for the screenshot group JSON file.
        /// </summary>
        /// <param name="gameId">Id of the game</param>
        /// <param name="providerId">Id of the provider</param>
        /// <param name="groupId">Id of the group</param>
        /// <returns>The generated file name</returns>
        internal static string GenerateFileName(Guid gameId, Guid providerId, Guid groupId)
        {
            if (!IsScreenshotUtilitiesInstalled)
            {
                return string.Empty;
            }

            var directoryInfo = GetDownloadPath(gameId: gameId, providerId: providerId);

            return directoryInfo is null || !directoryInfo.Exists ? string.Empty : Path.Combine(directoryInfo.FullName, $"{groupId}.json");
        }

        /// <summary>
        /// Loads the screenshot group from file or creates a new one if it doesn't exist.
        /// </summary>
        /// <param name="game">The game associated with the screenshot group.</param>
        /// <param name="providerName">The name of the screenshot provider.</param>
        /// <param name="providerId">The ID of the screenshot provider.</param>
        /// <param name="categoryName">The name of the screenshot category.</param>
        /// <param name="categoryId">The ID of the screenshot category.</param>
        /// <returns>
        /// A tuple indicating whether the group was loaded from file and the loaded or newly
        /// created screenshot group.
        /// </returns>
        internal static (bool, ScreenshotGroup) LoadGroup(Game game, string providerName, Guid providerId, string categoryName = default, Guid categoryId = default)
        {
            categoryName = string.IsNullOrEmpty(categoryName) ? providerName : categoryName;
            categoryId = categoryId == default ? providerId : categoryId;

            var screenshotGroup = ScreenshotGroup.CreateFromFile(new FileInfo(GenerateFileName(game.Id, providerId, categoryId)));

            if (screenshotGroup == null)
            {
                screenshotGroup = new ScreenshotGroup(categoryName, categoryId)
                {
                    Provider = new ScreenshotProvider(providerName, providerId),
                    Screenshots = new RangeObservableCollection<Screenshot>()
                };

                return (false, screenshotGroup);
            }

            return (true, screenshotGroup);
        }

        /// <summary>
        /// Removes the screenshots for the specified game and provider.
        /// </summary>
        /// <param name="game">The game associated with the screenshots.</param>
        /// <param name="showNotification">Whether to show a notification if the deletion fails.</param>
        /// <param name="providerId">
        /// The ID of the provider. When set to default the screenshots of all providers will be removed.
        /// </param>
        /// <returns>True if the screenshots were successfully removed; otherwise, false.</returns>
        internal static bool RemoveScreenshots(Game game, bool showNotification = true, Guid providerId = default)
        {
            var path = GetDownloadPath(gameId: game.Id, providerId: providerId, createDir: false);
            var succeeded = false;

            try
            {
                if (path.Exists)
                {
                    path.Delete(true);
                }

                Task.Delay(TimeSpan.FromMilliseconds(100));

                path.Refresh();

                succeeded = !path.Exists;
            }
            catch (Exception ex)
            {
                succeeded = false;
                Log.Error(ex, $"Couldn't delete folder for game \"{game.Name}\" ({game.Id})");
            }

            if (!succeeded && showNotification)
            {
                API.Instance.Notifications.Add(new NotificationMessage($"ScreenshotUtilitiesDelete{path.Name}",
                    string.Format(ResourceProvider.GetString("LOCScreenshotUtilitiesNotificationFolderNotDeleted"), game.Name),
                    NotificationType.Error,
                    () => Process.Start("explorer.exe", path.FullName)));
            }

            return succeeded;
        }

        /// <summary>
        /// Saves the screenshot group to a JSON file.
        /// </summary>
        /// <param name="game">The game associated with the screenshot group.</param>
        /// <param name="group">The screenshot group to save.</param>
        internal static void SaveScreenshotGroupJson(Game game, ScreenshotGroup group)
        {
            if (!IsScreenshotUtilitiesInstalled
                || game == null
                || group == null
                || group.Provider == null
                || group.Provider.Id == null
                || group.Provider.Id == Guid.Empty)
            {
                return;
            }

            group.FileName = GenerateFileName(game.Id, group.Provider.Id, group.Id);
            group.LastUpdate = DateTime.Now;
            group.Save();
        }
    }
}
