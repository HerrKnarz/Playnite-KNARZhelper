using ImageMagick;
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

            using (var image = new MagickImage(imageFileName))
            {
                image.Scale(0, (uint)thumbNailHeight);

                image.Format = MagickFormat.Jpg;

                await image.WriteAsync(thumbnailFileName);
            }

            return new FileInfo(thumbnailFileName);
        }
    }
}
