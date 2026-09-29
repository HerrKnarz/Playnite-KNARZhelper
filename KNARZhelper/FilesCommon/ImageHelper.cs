using ImageMagick;
using System;

namespace KNARZhelper.FilesCommon
{
    /// <summary>
    /// Helper class for image operations.
    /// </summary>
    internal static class ImageHelper
    {
        public static readonly string[] SupportedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".bmp" };

        public static bool MirrorImage(string imageFileName, bool horizontal)
        {
            try
            {
                using (var image = new MagickImage(imageFileName))
                {
                    if (horizontal)
                    {
                        image.Flop();
                    }
                    else
                    {
                        image.Flip();
                    }

                    image.Write(imageFileName);
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error mirroring image file {imageFileName}");
                return false;
            }
        }

        public static bool RotateImage(string imageFileName, int rotationAngle)
        {
            try
            {
                using (var image = new MagickImage(imageFileName))
                {
                    image.Rotate(rotationAngle);
                    image.Write(imageFileName);
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error rotating image file {imageFileName}");
                return false;
            }
        }
    }
}
