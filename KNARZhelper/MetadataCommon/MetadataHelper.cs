using Playnite.SDK.Models;
using System.Linq;
using System.Text.RegularExpressions;

namespace KNARZhelper.MetadataCommon
{
    internal class MetadataHelper
    {
        public static Link GetLink(Game game, Regex urlMask)
        {
            return game == null || urlMask == null
                ? null
                : (game.Links?.FirstOrDefault(l => l.Url != null && urlMask.IsMatch(l.Url)));
        }
    }
}