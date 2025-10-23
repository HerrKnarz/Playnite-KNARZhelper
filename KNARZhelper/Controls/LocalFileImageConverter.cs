using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace KNARZhelper.Controls
{
    public class LocalFileImageConverter : IValueConverter
    {
        public static Dictionary<string, BitmapImage> CachedBitmapImages = new Dictionary<string, BitmapImage>();

        public static void ClearCachedImages() => CachedBitmapImages = new Dictionary<string, BitmapImage>();

        public object Convert(object value, Type targetType,
                              object parameter, System.Globalization.CultureInfo culture)
        {
            var val = value as string;

            if (!string.IsNullOrEmpty(val))
            {
                if (val.IsValidHttpUrl())
                {
                    return val;
                }

                val = ((string)value).ToLower();

                if (CachedBitmapImages.TryGetValue(val, out var bi))
                {
                    return bi;
                }

                try
                {
                    using (var fstream = new FileStream(value.ToString(), FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        bi = new BitmapImage();
                        bi.BeginInit();
                        bi.CacheOption = BitmapCacheOption.OnLoad;
                        bi.StreamSource = fstream;
                        bi.StreamSource.Flush();
                        bi.EndInit();
                        bi.Freeze();

                        bi.StreamSource.Dispose();
                    }
                    CachedBitmapImages.Add(val, bi);
                    return bi;
                }
                catch
                {
                }

                return null;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException("LocalFileImageConverter: Two way conversion is not supported.");
    }
}
