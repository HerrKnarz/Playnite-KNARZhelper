using ImageMagick;
using System;
using System.IO;
using System.Threading.Tasks;

namespace KNARZhelper.FilesCommon
{
    /// <summary>
    /// Helper class for image operations.
    /// </summary>
    internal static class ImageHelper
    {
        /// <summary>
        /// Creates a thumbnail image with a height of 120 pixels, maintaining the aspect ratio.
        /// </summary>
        /// <param name="imageFileName">The path to the original image file.</param>
        /// <param name="thumbNailHeight">Height of the thumbnails that will be generated</param>
        /// <param name="thumbnailFileName">The path to the thumbnail image file.</param>
        /// <returns>The FileInfo of the created thumbnail image.</returns>
        public static async Task<FileInfo> CreateThumbnailImage(string imageFileName, int thumbNailHeight, string thumbnailFileName = "")
        {
            // We exit the method for jxr files and videos, because ImageMagick can't process
            // themout of the box without serious fiddling or in case of videos at all.
            if (FileHelper.GetFileExtensionFromUrl(imageFileName).IsOneOf(".jxr", ".mp4", ".avi", ".webm"))
            {
                return null;
            }

            if (string.IsNullOrEmpty(thumbnailFileName))
            {
                var fileInfo = new FileInfo(imageFileName);
                thumbnailFileName = Path.Combine(fileInfo.DirectoryName, $"{Path.GetFileNameWithoutExtension(fileInfo.Name)}_thumb.jpg");
            }

            var thumbnailFileInfo = new FileInfo(thumbnailFileName);

            if (thumbnailFileInfo.Exists)
            {
                thumbnailFileInfo.Delete();
            }

            var imageBytes = imageFileName.IsValidHttpUrl()
                ? await FileDownloader.Instance().DownloadFileAsync(new Uri(imageFileName))
                : File.ReadAllBytes(imageFileName);

            try
            {
                using (var image = new MagickImage(imageBytes))
                {
                    image.Scale(0, (uint)thumbNailHeight);

                    image.Format = MagickFormat.Jpg;

                    await image.WriteAsync(thumbnailFileName);
                }

                return new FileInfo(thumbnailFileName);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error processing file {imageFileName}");
            }

            return null;
        }
    }
}
