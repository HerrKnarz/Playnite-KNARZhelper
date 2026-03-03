using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KNARZhelper.ScreenshotsCommon.Models
{
    /// <summary>
    /// Media file functionality for the ScreenshotGroups class. Usually not needed by provider addons.
    /// </summary>
    public partial class ScreenshotGroups : ObservableCollection<ScreenshotGroup>
    {
        /// <summary>
        /// Downloads all screenshots in all groups.
        /// </summary>
        /// <param name="thumbNailHeight">Height of the thumbnails that will be generated</param>
        /// <param name="providerGuid">
        /// Optional provider GUID to filter which groups to download from.
        /// </param>
        /// <returns>True if new screenshots were downloaded.</returns>
        public async Task<bool> DownloadAllAsync(int thumbNailHeight, Guid providerGuid = default)
        {
            var bag = new ConcurrentBag<bool>();
            var maxParallel = 2;
            var throttler = new SemaphoreSlim(initialCount: maxParallel);
            var tasks = this.Where(g => providerGuid == default || g.Provider.Id.Equals(providerGuid)).Select(async group =>
            {
                await throttler.WaitAsync();
                try
                {
                    bag.Add(await group.DownloadAsync(thumbNailHeight));
                }
                finally
                {
                    throttler.Release();
                }
            });
            await Task.WhenAll(tasks);

            return bag.Any(x => x);
        }

        /// <summary>
        /// Refreshes all screenshots in all groups.
        /// </summary>
        /// <param name="thumbNailHeight">Height of the thumbnails that will be generated</param>
        /// <param name="providerId">Optional provider GUID to filter which groups to refresh.</param>
        /// <returns>True if new thumbnails were generated.</returns>
        public async Task<bool> RefreshAllThumbnailsAsync(int thumbNailHeight, Guid providerId = default)
        {
            var generated = false;

            foreach (var group in this.Where(g => providerId == default || g.Provider.Id.Equals(providerId)))
            {
                generated |= await group.RefreshThumbnailsAsync(thumbNailHeight);
                group.Save();
            }

            return generated;
        }
    }
}
