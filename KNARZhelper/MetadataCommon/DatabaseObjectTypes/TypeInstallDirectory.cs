using KNARZhelper.MetadataCommon.Enum;
using Playnite.SDK;
using Playnite.SDK.Models;
using System.Linq;

namespace KNARZhelper.MetadataCommon.DatabaseObjectTypes
{
    public class TypeInstallDirectory : BaseStringType
    {
        public override bool CanBeClearedInGame => false;
        public override bool IsDefaultToCopy => false;
        public override string LabelSingular => ResourceProvider.GetString("LOCGameInstallDirTitle");
        public override FieldType Type => FieldType.InstallDirectory;

        public override bool AddValueToGame(Game game, string value)
        {
            API.Instance.MainView.UIDispatcher.Invoke(() =>
            {
                game.InstallDirectory = value;
            });

            return true;
        }

        public override void EmptyFieldInGame(Game game) => API.Instance.MainView.UIDispatcher.Invoke(() => game.InstallDirectory = default);

        public override bool FieldInGameIsEmpty(Game game) => !game?.InstallDirectory?.Trim().Any() ?? true;

        public override bool GameContainsValue(Game game, string value) => value != null && (game?.InstallDirectory?.RegExIsMatch(value) ?? false);

        public override string GetValue(Game game) => game.InstallDirectory;
    }
}
