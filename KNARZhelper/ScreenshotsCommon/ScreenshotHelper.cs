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

        internal static string GenerateFileName(Guid gameId, Guid providerId, Guid groupId)
        {
            if (!IsScreenshotUtilitiesInstalled)
            {
                return string.Empty;
            }

            var directoryInfo = GetDownloadPath(gameId: gameId, providerId: providerId);

            if (directoryInfo is null || !directoryInfo.Exists)
            {
                return string.Empty;
            }

            return Path.Combine(directoryInfo.FullName, $"{groupId}.json");
        }

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

        internal static bool IsScreenshotUtilitiesInstalled => API.Instance.Addons.Plugins.Exists(p => p.Id == ScreenshotUtilitiesId);

        internal static (bool, ScreenshotGroup) LoadGroups(Game game, string providerName, Guid providerId, string categoryName = default, Guid categoryId = default)
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

        internal static bool RemoveScreenshots(Game game, bool showNotification = true)
        {
            var path = GetDownloadPath(gameId: game.Id, createDir: false);
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
