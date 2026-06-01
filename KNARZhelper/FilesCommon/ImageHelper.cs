using ImageMagick;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace KNARZhelper.FilesCommon
{
    /// <summary>
    /// Helper class for image operations.
    /// </summary>
    internal static class ImageHelper
    {
        public static readonly string[] SupportedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".bmp" };
        public static readonly string[] SupportedVideoExtensions = { ".mp4", ".avi", ".webm", ".wmv", ".mov" };

        /// <summary>
        /// Creates a thumbnail image.
        /// </summary>
        /// <param name="imageFileName">The path to the original image file.</param>
        /// <param name="thumbNailHeight">Height of the thumbnails that will be generated</param>
        /// <param name="thumbnailFileName">The path to the thumbnail image file.</param>
        /// <returns>The FileInfo of the created thumbnail image.</returns>
        public static async Task<FileInfo> CreateThumbnailImage(string imageFileName, int thumbNailHeight, string thumbnailFileName = "")
        {
            //NEXT: Check why the thumbnail file seems to be used. Does a FileInfo lock a file maybe?

            byte[] imageBytes = null;
            var videoInitialized = false;

            if (SupportedImageExtensions.Contains(FileHelper.GetFileExtensionFromUrl(imageFileName)))
            {
                imageBytes = imageFileName.IsValidHttpUrl()
                    ? await FileDownloader.Instance().DownloadFileAsync(new Uri(imageFileName))
                    : File.ReadAllBytes(imageFileName);
            }

            if (string.IsNullOrEmpty(thumbnailFileName))
            {
                var fileInfo = new FileInfo(imageFileName);
                thumbnailFileName = Path.Combine(fileInfo.DirectoryName, $"{Path.GetFileNameWithoutExtension(fileInfo.Name)}_thumb.jpg");
            }

            var thumbnailFileInfo = new FileInfo(thumbnailFileName);

            if (SupportedVideoExtensions.Contains(FileHelper.GetFileExtensionFromUrl(imageFileName)))
            {
                try
                {
                    var ffMpeg = new NReco.VideoConverter.FFMpegConverter();

                    ffMpeg.GetVideoThumbnail(imageFileName, thumbnailFileInfo.FullName, 5);

                    Task.Delay(TimeSpan.FromMilliseconds(100));

                    videoInitialized = true;

                    thumbnailFileInfo.Refresh();

                    if (!thumbnailFileInfo.Exists)
                    {
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Error processing video {imageFileName}");
                }
            }

            if (!videoInitialized && imageBytes is null)
            {
                return null;
            }

            try
            {
                using (var image = videoInitialized ? new MagickImage(thumbnailFileInfo.FullName) : new MagickImage(imageBytes))
                {
                    image.Scale(0, (uint)thumbNailHeight);

                    image.Format = MagickFormat.Jpg;

                    if (thumbnailFileInfo.Exists)
                    {
                        thumbnailFileInfo.Delete();
                        Task.Delay(TimeSpan.FromMilliseconds(100));
                    }

                    await image.WriteAsync(thumbnailFileName);
                }

                return new FileInfo(thumbnailFileName);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error processing image file {imageFileName}");
            }

            return null;
        }
    }
}
