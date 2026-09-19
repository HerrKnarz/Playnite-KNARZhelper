using Playnite.SDK.Models;
using System.Drawing;

namespace KNARZhelper.MetadataCommon.DatabaseObjectTypes
{
    public interface IImageType
    {
        float GetAspectRatio(Game game);

        string GetExtension(Game game);

        int GetFileSizeInBytes(Game game);

        Size GetImageSize(Game game);
    }
}
