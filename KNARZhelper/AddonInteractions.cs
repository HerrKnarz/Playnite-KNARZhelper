using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace KNARZhelper
{
    public static class AddonInteractions
    {
        public static bool IsExtraMetadataLoaderInstalled() => API.Instance.Addons.Plugins.Exists(p => p.Id == Guid.Parse("705fdbca-e1fc-4004-b839-1d040b8b4429"));

        public static bool SetImageAsLogo(Game game, string image)
        {
            if (image == null)
            {
                return false;
            }

            var logoFile = new FileInfo(image);

            if (!logoFile.Exists)
            {
                return false;
            }

            var logoPath = Path.Combine(API.Instance.Paths.ConfigurationPath, "ExtraMetadata", "games", game.Id.ToString().ToLower());

            var logoDir = new DirectoryInfo(logoPath);

            if (!logoDir.Exists)
            {
                logoDir.Create();
            }
            else
            {
                var existingFiles = logoDir.GetFiles("Logo.*");

                existingFiles.ForEach(f => f.Delete());
            }

            var destinationPath = Path.Combine(logoDir.FullName, $"Logo{logoFile.Extension}");

            var createdLogo = logoFile.CopyTo(destinationPath);

            // We wait a bit to ensure the file system has updated
            Task.Delay(TimeSpan.FromMilliseconds(100));

            return createdLogo.Exists;
        }
    }
}
