using KNARZhelper.MetadataCommon.Enum;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace KNARZhelper.MetadataCommon.DatabaseObjectTypes
{
    public class TypeLogo : BaseMediaType
    {
        public override bool CanBeSetByMetadataAddOn => false;
        public override string LabelSingular => ResourceProvider.GetString("LOCKNARZHelperLogoTitle");

        public override FieldType Type => FieldType.Logo;

        public override void EmptyFieldInGame(Game game) => AddonInteractions.RemoveLogo(game);

        public override string GetFile(Game game) => AddonInteractions.GetLogo(game);

        internal override string GetValue(Game game) => AddonInteractions.GetLogo(game);

        internal override void SetValue(Game game, string value) => AddonInteractions.SetImageAsLogo(game, value);
    }
}
