using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace KNARZhelper
{
    public static class StringFormatter
    {
        private static readonly Guid _gogId = Guid.Parse("aebe8b7c-6dc3-4a66-af31-e7375c6b5e9e");
        private static readonly Guid _gogOssId = Guid.Parse("03689811-3F33-4DFB-A121-2EE168FB9A5C");
        private static readonly string _placeholderGameName = "{GameName}";
        private static readonly string _placeholderGogId = "{GogId}";
        private static readonly string _placeholderRomName = "{RomName}";
        private static readonly string _placeholderSteamId = "{SteamId}";

        public static string FormatString(this string str,
            StringFormatParameters formatParams)
        {
            if (formatParams.RemoveEditionSuffix)
            {
                str = str.RemoveEditionSuffix();
            }

            if (formatParams.RemoveHyphens)
            {
                str = str.Replace("-", "");
            }

            if (formatParams.UnderscoresToWhitespaces)
            {
                str = str.Replace("_", " ");
            }

            if (formatParams.RemoveSpecialChars)
            {
                str = str.RemoveSpecialChars();
            }

            if (formatParams.RemoveDiacritics)
            {
                str = str.RemoveDiacritics();
            }

            if (formatParams.ToTitleCase)
            {
                str = str.ToTitleCase();
            }

            if (formatParams.ToLower)
            {
                str = str.ToLower();
            }

            str = formatParams.RemoveWhitespaces ? str.Replace(" ", "") : str.CollapseWhitespaces();

            if (formatParams.WhitespacesToHyphens)
            {
                str = str.Replace(" ", "-");
            }

            if (formatParams.WhitespacesToUnderscores)
            {
                str = str.Replace(" ", "_");
            }

            if (formatParams.ReplaceInvalidFileNameChars)
            {
                str = str.ReplaceInvalidFileNameChars(formatParams.InvalidCharReplacement);
            }

            if (formatParams.EscapeDataString)
            {
                str = str.EscapeDataString();
            }

            if (formatParams.UrlEncode)
            {
                str = str.UrlEncode();
            }

            return str;
        }

        public static string ReplaceGameNamePlaceholder(this string str, string gameName)
        {
            return string.IsNullOrEmpty(str) || !str.Contains(_placeholderGameName)
                ? str
                : string.IsNullOrEmpty(gameName) ? string.Empty : str.Replace(_placeholderGameName, gameName);
        }

        public static string ReplaceGogPlaceholder(this string str, Game game)
        {
            return string.IsNullOrEmpty(str) || !str.Contains(_placeholderGogId) ? str
                : !game.PluginId.IsOneOf(_gogId, _gogOssId) ? string.Empty
                : str.Replace(_placeholderGogId, game.GameId);
        }

        public static string ReplacePlaceholders(this string str, Game game, string gameName = null)
        {
            if (string.IsNullOrEmpty(str) || !str.Contains('{'))
            {
                return str;
            }

            if (game == null)
            {
                game = new Game();
            }

            if (!string.IsNullOrEmpty(gameName))
            {
                gameName = game.Name;
            }

            return API.Instance.ExpandGameVariables(game, str)
                .ReplaceGameNamePlaceholder(gameName)
                .ReplaceSteamPlaceholder(game)
                .ReplaceGogPlaceholder(game)
                .ReplaceRomNamePlaceholder(game);
        }

        public static string ReplaceRomNamePlaceholder(this string str, Game game)
        {
            return string.IsNullOrEmpty(str) || !str.Contains(_placeholderRomName) ? str
                : game.IsInstalled && (game.Roms?.Any() ?? false)
                    ? str.Replace(_placeholderRomName, Path.GetFileNameWithoutExtension(game.Roms[0].Path))
                    : string.Empty;
        }

        public static string ReplaceSteamPlaceholder(this string str, Game game)
        {
            if (string.IsNullOrEmpty(str) || !str.Contains(_placeholderSteamId))
            {
                return str;
            }

            var steamId = SteamHelper.GetSteamId(game);

            return string.IsNullOrEmpty(steamId) ? string.Empty : str.Replace(_placeholderSteamId, steamId);
        }
    }

    public class StringFormatParameters
    {
        public bool EscapeDataString { get; set; } = false;
        public string InvalidCharReplacement { get; set; } = null;
        public bool RemoveDiacritics { get; set; } = false;
        public bool RemoveEditionSuffix { get; set; } = false;
        public bool RemoveHyphens { get; set; } = false;
        public bool RemoveSpecialChars { get; set; } = false;
        public bool RemoveWhitespaces { get; set; } = false;
        public bool ReplaceInvalidFileNameChars { get; set; } = false;
        public bool ToLower { get; set; } = false;
        public bool ToTitleCase { get; set; } = false;
        public bool UnderscoresToWhitespaces { get; set; } = false;
        public bool UrlEncode { get; set; } = false;
        public bool WhitespacesToHyphens { get; set; } = false;
        public bool WhitespacesToUnderscores { get; set; } = false;
    }
}
