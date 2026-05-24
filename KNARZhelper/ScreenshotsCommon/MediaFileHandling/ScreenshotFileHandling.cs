using KNARZhelper.FilesCommon;
using KNARZhelper.MetadataCommon.Enum;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace KNARZhelper.ScreenshotsCommon.Models
{
    /// <summary>
    /// Media file functionality for the Screenshot class. Usually not needed by provider addons.
    /// </summary>
    public partial class Screenshot : ObservableObject
    {
        /// <summary>
        /// Specifies if the screenshot can be opened locally.
        /// </summary>
        [DontSerialize]
        public bool CanBeOpened => IsDownloaded || IsLocal;

        /// <summary>
        /// Specifies whether the screenshot's origin is local.
        /// </summary>
        [DontSerialize]
        public bool IsLocal => File.Exists(Path);

        /// <summary>
        /// Copies the screenshot image to the clipboard.
        /// </summary>
        public void CopyToClipboard()
        {
            if (!CanBeOpened)
            {
                return;
            }

            var fileInfo = new FileInfo(DisplayPath);

            if (fileInfo.Exists)
            {
                try
                {
                    Clipboard.SetImage(BitmapFrame.Create(new Uri(DisplayPath, UriKind.Absolute)));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to copy screenshot to clipboard: {ex}");
                }
            }
        }

        /// <summary>
        /// Downloads the screenshot to the specified path.
        /// </summary>
        /// <param name="path">Path to the folder where the screenshot will be downloaded.</param>
        /// <returns>True if new screenshots were downloaded.</returns>
        public async Task<bool> DownloadAsync(string path)
        {
            if (!PathIsUrl || IsDownloaded)
            {
                return false;
            }

            try
            {
                path = GetDownloadPath(path);
                var image = await FileDownloader.Instance().DownloadFileAsync(path, new Uri(Path));
                DownloadedPath = image.FullName;

                return true;
            }
            catch (Exception exception)
            {
                Log.Error(exception, $"Error trying to download file from {path}");
                return false;
            }
        }

        /// <summary>
        /// Generates a thumbnail for the downloaded screenshot.
        /// </summary>
        /// <param name="thumbNailHeight">Height of the thumbnails that will be generated</param>
        /// <param name="replaceExisting">When true existing thumbnails will be regenerated</param>
        /// <param name="alwaysCreateThumbnails">
        /// <param name="path">Optional path for the generated thumbnails. Is only needed when
        /// alwaysCreateThumbnails is true.</param> Flag to always create thumbnails, even if they
        /// already exist
        /// </param>
        /// <returns>True if new thumbnails were generated.</returns>
        public async Task<bool> GenerateThumbnailAsync(int thumbNailHeight, bool replaceExisting = false, bool alwaysCreateThumbnails = false, string path = default)
        {
            if (
                (!alwaysCreateThumbnails
                    && (!IsDownloaded
                        || !File.Exists(DownloadedPath)))
                || (!string.IsNullOrEmpty(DownloadedThumbnailPath)
                    && !replaceExisting))
            {
                return false;
            }

            var thumbnailPath = string.Empty;

            // We need to generate the thumbnail filename if there is no downloaded screenshot to
            // get it from.
            if (alwaysCreateThumbnails && !File.Exists(DownloadedPath))
            {
                thumbnailPath = GetDownloadPath(path, true);
            }

            try
            {
                var thumb = await ImageHelper.CreateThumbnailImage(DisplayPath, thumbNailHeight, thumbnailPath);
                DownloadedThumbnailPath = thumb.FullName;
                return true;
            }
            catch (Exception exception)
            {
                Log.Error(exception, $"Error trying to generate thumbnail for {DownloadedPath}");
                return false;
            }
        }

        /// <summary>
        /// Opens the folder containing the screenshot in File Explorer.
        /// </summary>
        public void OpenContainingFolder()
        {
            if (!CanBeOpened)
            {
                return;
            }

            if (new FileInfo(DisplayPath).Directory.Exists)
            {
                Process.Start("explorer.exe", $"/select, \"{DisplayPath}\"");

                return;
            }
        }

        /// <summary>
        /// Opens the screenshot in its associated application.
        /// </summary>
        public void OpenInAssociatedApplication()
        {
            if (!CanBeOpened)
            {
                return;
            }

            var fileInfo = new FileInfo(DisplayPath);

            if (fileInfo.Exists)
            {
                Process.Start(new ProcessStartInfo(fileInfo.FullName) { UseShellExecute = true });
            }
        }

        /// <summary>
        /// Opens the screenshot URL in the default web browser.
        /// </summary>
        public void OpenInBrowser()
        {
            if (PathIsUrl)
            {
                Process.Start(new ProcessStartInfo(Path));
            }
        }

        public void SetAs(Game game, FieldType mediaType = FieldType.Background)
        {
            if (!CanBeOpened)
            {
                return;
            }

            API.Instance.MainView.UIDispatcher.Invoke(delegate
            {
                var image = mediaType == FieldType.Logo ? DisplayPath : API.Instance.Database.AddFile(DisplayPath, game.Id);

                switch (mediaType)
                {
                    case FieldType.Background:
                        game.BackgroundImage = image;
                        break;

                    case FieldType.Cover:
                        game.CoverImage = image;
                        break;

                    case FieldType.Icon:
                        game.Icon = image;
                        break;

                    case FieldType.Logo:
                        AddonInteractions.SetImageAsLogo(game, image);
                        break;

                    default:
                        throw new NotSupportedException($"Media type {mediaType} is not supported.");
                }

                API.Instance.Database.Games.Update(game);
            });
        }

        private string GetDownloadPath(string path, bool thumbNailPath = false)
        {
            var suffix = thumbNailPath ? "_thumb.jpg" : FileHelper.GetFileExtensionFromUrl(Path);

            return System.IO.Path.Combine(path, $"{Id}{suffix}");
        }
    }
}
