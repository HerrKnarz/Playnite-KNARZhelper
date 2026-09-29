using ImageMagick;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace KNARZhelper.FilesCommon
{
    /// <summary>
    /// Helper class for image operations.
    /// </summary>
    internal static class ImageHelper
    {
        public static readonly string[] SupportedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".bmp" };

        public static Size GetImageSize(string imageFileName)
        {
            try
            {
                if (imageFileName.IsNullOrEmpty())
                {
                    return Size.Empty;
                }

                using (var image = new MagickImage(imageFileName))
                {
                    return new Size()
                    {
                        Width = (int)image.Width,
                        Height = (int)image.Height
                    };
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error getting image size for {imageFileName}");
            }

            return Size.Empty;
        }

        public static bool MirrorImage(string imageFileName, bool horizontal)
        {
            try
            {
                if (!SupportedImageExtensions.Contains(Path.GetExtension(imageFileName).ToLower()))
                {
                    Log.Debug($"Skipping mirroring for unsupported image file {imageFileName}");
                    return false;
                }

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
                if (!SupportedImageExtensions.Contains(Path.GetExtension(imageFileName).ToLower()))
                {
                    Log.Debug($"Skipping rotating for unsupported image file {imageFileName}");
                    return false;
                }

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
