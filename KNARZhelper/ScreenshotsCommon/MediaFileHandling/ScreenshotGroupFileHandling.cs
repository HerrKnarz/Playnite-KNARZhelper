using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KNARZhelper.ScreenshotsCommon.Models
{
    /// <summary>
    /// Media file functionality for the ScreenshotGroup class. Usually not needed by provider addons.
    /// </summary>
    public partial class ScreenshotGroup : ObservableObject
    {
        /// <summary>
        /// Downloads all screenshots in the group.
        /// </summary>
        /// <param name="thumbNailHeight">Height of the thumbnails that will be generated</param>
        /// <returns>True if new screenshots were downloaded.</returns>
        public async Task<bool> DownloadAsync(int thumbNailHeight)
        {
            try
            {
                var bag = new ConcurrentBag<bool>();
                var maxParallel = 4;
                var throttler = new SemaphoreSlim(initialCount: maxParallel);
                var tasks = Screenshots.Select(async screenshot =>
                {
                    await throttler.WaitAsync();
                    try
                    {
                        var response = await screenshot.DownloadAsync(BasePath);
                        response |= await screenshot.GenerateThumbnailAsync(thumbNailHeight);
                        bag.Add(response);
                    }
                    finally
                    {
                        throttler.Release();
                    }
                });
                await Task.WhenAll(tasks);

                if (bag.Count > 0)
                {
                    Save();
                }

                return bag.Any(x => x);
            }
            catch (Exception ex)
            {
                Log.Error(ex);

                return false;
            }
        }

        /// <summary>
        /// Opens the path to the json file in windows explorer.
        /// </summary>
        public void OpenContainingFolder() => Process.Start("explorer.exe", BasePath);

        /// <summary>
        /// Creates thumbnails to all screenshots in the group and regenerates already existing ones.
        /// </summary>
        /// <param name="thumbNailHeight">Height of the thumbnails that will be generated</param>
        /// <returns>True if new thumbnails were generated.</returns>
        public async Task<bool> RefreshThumbnailsAsync(int thumbNailHeight)
        {
            var generated = false;

            try
            {
                foreach (var screenshot in Screenshots)
                {
                    generated |= await screenshot.GenerateThumbnailAsync(thumbNailHeight, true);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }

            return generated;
        }
    }
}
