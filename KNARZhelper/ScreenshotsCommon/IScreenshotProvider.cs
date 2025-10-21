using Playnite.SDK.Models;
using System.Threading.Tasks;

namespace KNARZhelper.ScreenshotsCommon
{
    public interface IScreenshotProvider
    {
        Task<bool> GetScreenshotsAsync(Game game);
    }
}
