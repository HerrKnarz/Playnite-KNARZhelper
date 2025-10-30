using KNARZhelper.MetadataCommon.Enum;
using Playnite.SDK;
using Playnite.SDK.Models;
using System.Linq;

namespace KNARZhelper.MetadataCommon.DatabaseObjectTypes
{
    internal class TypeSortingName : BaseStringType
    {
        public override bool IsDefaultToCopy => false;
        public override string LabelSingular => ResourceProvider.GetString("LOCGameSortingNameTitle");
        public override FieldType Type => FieldType.SortingName;

        public override bool AddValueToGame(Game game, string value)
        {
            API.Instance.MainView.UIDispatcher.Invoke(() =>
            {
                game.SortingName = value;
            });

            return true;
        }

        public override void EmptyFieldInGame(Game game) => API.Instance.MainView.UIDispatcher.Invoke(() => game.SortingName = default);

        public override bool FieldInGameIsEmpty(Game game) => !game?.SortingName?.Trim().Any() ?? true;

        public override bool GameContainsValue(Game game, string value) => value != null && (game?.SortingName?.RegExIsMatch(value) ?? false);

        public override string GetValue(Game game) => game.SortingName;
    }
}