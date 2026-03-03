using KNARZhelper.FilesCommon;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
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
        /// Copies the screenshot image to the clipboard.
        /// </summary>
        public void CopyToClipboard()
        {
            if (string.IsNullOrEmpty(DownloadedPath))
            {
                return;
            }

            var fileInfo = new FileInfo(DownloadedPath);

            if (fileInfo.Exists)
            {
                try
                {
                    Clipboard.SetImage(BitmapFrame.Create(new Uri(DownloadedPath, UriKind.Absolute)));
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
                path = System.IO.Path.Combine(path, $"{Id}{FileHelper.GetFileExtensionFromUrl(Path)}");
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
        /// <returns>True if new thumbnails were generated.</returns>
        public async Task<bool> GenerateThumbnailAsync(int thumbNailHeight, bool replaceExisting = false)
        {
            if (!IsDownloaded || !File.Exists(DownloadedPath)
                || (!string.IsNullOrEmpty(DownloadedThumbnailPath) && !replaceExisting))
            {
                return false;
            }

            try
            {
                var thumb = await ImageHelper.CreateThumbnailImage(DownloadedPath, thumbNailHeight);
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
            if (string.IsNullOrEmpty(DownloadedPath))
            {
                return;
            }

            if (new FileInfo(DownloadedPath).Directory.Exists)
            {
                Process.Start("explorer.exe", $"/select, \"{DownloadedPath}\"");
            }
        }

        /// <summary>
        /// Opens the screenshot in its associated application.
        /// </summary>
        public void OpenInAssociatedApplication()
        {
            if (string.IsNullOrEmpty(DownloadedPath))
            {
                return;
            }

            var fileInfo = new FileInfo(DownloadedPath);

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

        public void SetAs(Game game, MetadataField mediaType = MetadataField.BackgroundImage)
        {
            if (string.IsNullOrEmpty(DownloadedPath))
            {
                return;
            }

            API.Instance.MainView.UIDispatcher.Invoke(delegate
            {
                var image = API.Instance.Database.AddFile(DownloadedPath, game.Id);

                switch (mediaType)
                {
                    case MetadataField.BackgroundImage:
                        game.BackgroundImage = image;
                        break;

                    case MetadataField.CoverImage:
                        game.CoverImage = image;
                        break;

                    case MetadataField.Icon:
                        game.Icon = image;
                        break;

                    default:
                        throw new NotSupportedException($"Media type {mediaType} is not supported.");
                }

                API.Instance.Database.Games.Update(game);
            });
        }
    }
}
