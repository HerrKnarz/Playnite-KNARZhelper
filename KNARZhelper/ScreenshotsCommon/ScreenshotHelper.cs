using KNARZhelper.ScreenshotsCommon.Models;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace KNARZhelper.ScreenshotsCommon
{
    internal static class ScreenshotHelper
    {
        internal static Guid ScreenshotUtilitiesId = Guid.Parse("485d682f-73e9-4d54-b16f-b8dd49e88f90");

        internal static string GenerateFileName(Guid gameId, Guid providerId, Guid groupId)
        {
            if (!IsScreenshotUtilitiesInstalled)
            {
                return string.Empty;
            }

            var directoryInfo = new DirectoryInfo(API.Instance.Addons.Plugins.Find(p => p.Id == ScreenshotUtilitiesId).GetPluginUserDataPath());

            if (!directoryInfo.Exists)
            {
                return string.Empty;
            }

            directoryInfo = directoryInfo
                .CreateSubdirectory(gameId.ToString())
                .CreateSubdirectory(providerId.ToString());

            return Path.Combine(directoryInfo.FullName, $"{groupId}.json");
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
