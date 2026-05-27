using System;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace KNARZhelper.Controls
{
    public static class ResourceHelper
    {
        public static void LoadIconFont(string pluginPath)
        {
            if (Application.Current == null || Application.Current.Resources.Contains("KNARZIconFont"))
            {
                return;
            }

            var fontFile = new FileInfo(Path.Combine(pluginPath, "Resources\\KNARZIconFont.ttf"));

            if (!fontFile.Exists)
            {
                Log.Info($"Font not found in {fontFile.FullName}");
                return;
            }

            var fontFamily = new FontFamily(new Uri(fontFile.FullName, UriKind.Absolute), "./#KNARZIconFont");

            Application.Current.Resources.Add("KNARZIconFont", fontFamily);
        }
    }
}
