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

        /// <summary>
        /// Formats the specified string according to the options provided in the format parameters.
        /// </summary>
        /// <remarks>
        /// The formatting options are applied in a specific order. Some options may override or
        /// interact with others; for example, whitespace transformations are applied after other
        /// character replacements. If multiple options are enabled that affect the same characters,
        /// the final result reflects the cumulative effect of all enabled options.
        /// </remarks>
        /// <param name="str">The input string to be formatted.</param>
        /// <param name="formatParams">
        /// An object specifying the formatting options to apply, such as removing hyphens,
        /// converting to title case, or replacing invalid file name characters. Cannot be null.
        /// </param>
        /// <returns>
        /// A new string that results from applying the specified formatting options to the input string.
        /// </returns>
        public static string FormatString(this string str, StringFormatParameters formatParams)
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

        /// <summary>
        /// Replaces the game name placeholder in the specified string with the provided game name.
        /// </summary>
        /// <param name="str">The string that may contain the game name placeholder to be replaced.</param>
        /// <param name="gameName">
        /// The game name to substitute for the placeholder. If null or empty, an empty string is returned.
        /// </param>
        /// <returns>
        /// A new string with the game name placeholder replaced by the specified game name. If the
        /// original string does not contain the placeholder, the original string is returned unchanged.
        /// </returns>
        public static string ReplaceGameNamePlaceholder(this string str, string gameName)
        {
            return string.IsNullOrEmpty(str) || !str.Contains(_placeholderGameName)
                ? str
                : string.IsNullOrEmpty(gameName) ? string.Empty : str.Replace(_placeholderGameName, gameName);
        }

        /// <summary>
        /// Replaces the GOG placeholder in the specified string with the GOG game identifier if applicable.
        /// </summary>
        /// <remarks>
        /// If the input string does not contain the GOG placeholder or is null or empty, the
        /// original string is returned. If the game is not associated with a GOG plugin, an empty
        /// string is returned.
        /// </remarks>
        /// <param name="str">The input string that may contain the GOG placeholder to be replaced.</param>
        /// <param name="game">The game instance providing the GOG game identifier and plugin information.</param>
        /// <returns>
        /// A string with the GOG placeholder replaced by the game's identifier if the game is a GOG
        /// game; otherwise, returns the original string or an empty string if the game is not a GOG game.
        /// </returns>
        public static string ReplaceGogPlaceholder(this string str, Game game)
        {
            return string.IsNullOrEmpty(str) || !str.Contains(_placeholderGogId) ? str
                : !game.PluginId.IsOneOf(_gogId, _gogOssId) ? string.Empty
                : str.Replace(_placeholderGogId, game.GameId);
        }

        /// <summary>
        /// Replaces placeholders in the specified string with values from the provided game
        /// instance and game name.
        /// </summary>
        /// <remarks>
        /// Placeholders are replaced based on the properties of the provided game instance and the
        /// specified game name. If a placeholder cannot be resolved, it may remain unchanged in the result.
        /// </remarks>
        /// <param name="str">The input string containing placeholders to be replaced.</param>
        /// <param name="game">
        /// The game instance whose properties are used to replace corresponding placeholders. If
        /// null, a new game instance is used.
        /// </param>
        /// <param name="gameName">
        /// An optional game name to use for replacing the game name placeholder. If null, the name
        /// from the game instance is used.
        /// </param>
        /// <returns>
        /// A string with all recognized placeholders replaced by their corresponding values.
        /// Returns the original string if it is null, empty, or contains no placeholders.
        /// </returns>
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

        /// <summary>
        /// Replaces the ROM name placeholder in the specified string with the file name of the
        /// first ROM associated with the given game, if available.
        /// </summary>
        /// <remarks>
        /// If the input string does not contain the ROM name placeholder, the original string is
        /// returned unchanged. If the game is not installed or has no ROMs, and the placeholder is
        /// present, an empty string is returned.
        /// </remarks>
        /// <param name="str">
        /// The input string that may contain the ROM name placeholder to be replaced.
        /// </param>
        /// <param name="game">
        /// The game whose ROM file name is used to replace the placeholder. Must not be null.
        /// </param>
        /// <returns>
        /// A string with the ROM name placeholder replaced by the file name of the first ROM if the
        /// game is installed and has at least one ROM; otherwise, returns the original string or an
        /// empty string if the placeholder is present but no ROM is available.
        /// </returns>
        public static string ReplaceRomNamePlaceholder(this string str, Game game)
        {
            return string.IsNullOrEmpty(str) || !str.Contains(_placeholderRomName) ? str
                : game.IsInstalled && (game.Roms?.Any() ?? false)
                    ? str.Replace(_placeholderRomName, Path.GetFileNameWithoutExtension(game.Roms[0].Path))
                    : string.Empty;
        }

        /// <summary>
        /// Replaces the Steam ID placeholder in the specified string with the Steam ID associated
        /// with the given game.
        /// </summary>
        /// <param name="str">
        /// The input string that may contain the Steam ID placeholder to be replaced.
        /// </param>
        /// <param name="game">The game instance used to retrieve the Steam ID for replacement.</param>
        /// <returns>
        /// A new string with the Steam ID placeholder replaced by the actual Steam ID. Returns the
        /// original string if the placeholder is not found or the input is null or empty. Returns
        /// an empty string if the Steam ID cannot be determined.
        /// </returns>
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

    /// <summary>
    /// Represents a set of options that control how string formatting operations are performed.
    /// </summary>
    /// <remarks>
    /// Use this class to specify various transformations and sanitization behaviors when formatting
    /// strings, such as removing diacritics, replacing invalid characters, or applying case
    /// conversions. Each property enables or disables a specific formatting rule. This class is
    /// typically used to configure string formatting utilities or helpers that require customizable behavior.
    /// </remarks>
    public class StringFormatParameters
    {
        /// <summary>
        /// Indicates whether data strings should be escaped using percent-encoding.
        /// </summary>
        public bool EscapeDataString { get; set; } = false;

        /// <summary>
        /// String that will replace invalid file name characters when the
        /// ReplaceInvalidFileNameChars option is enabled. If null, invalid characters will be
        /// removed instead of replaced.
        /// </summary>
        public string InvalidCharReplacement { get; set; } = null;

        /// <summary>
        /// Indicates whether diacritical marks (accents) should be removed from characters in the
        /// string. For example, "é" would be transformed to "e". This can be useful for creating
        /// more standardized or ASCII-only strings.
        /// </summary>
        public bool RemoveDiacritics { get; set; } = false;

        /// <summary>
        /// Indicates whether the edition suffix should be removed from the string. Is typically
        /// used for game names that include suffixes like "Deluxe Edition" or "Game of the Year
        /// Edition", where the edition information is not desired in the formatted output.
        /// </summary>
        public bool RemoveEditionSuffix { get; set; } = false;

        /// <summary>
        /// Indicates whether hyphens should be removed from the processed text.
        /// </summary>
        public bool RemoveHyphens { get; set; } = false;

        /// <summary>
        /// Indicates whether special characters are removed during processing.
        /// </summary>
        public bool RemoveSpecialChars { get; set; } = false;

        /// <summary>
        /// Indicates whether whitespace characters should be removed during processing.
        /// </summary>
        public bool RemoveWhitespaces { get; set; } = false;

        /// <summary>
        /// Indicates whether invalid characters in file names are automatically replaced.
        /// </summary>
        public bool ReplaceInvalidFileNameChars { get; set; } = false;

        /// <summary>
        /// Indicates whether the string should be converted to lowercase.
        /// </summary>
        public bool ToLower { get; set; } = false;

        /// <summary>
        /// Indicates whether the string should be converted to title case, where the first letter
        /// of each word is capitalized and the rest are lowercase.
        /// </summary>
        public bool ToTitleCase { get; set; } = false;

        /// <summary>
        /// Indicates whether underscores in the string should be replaced with whitespace characters.
        /// </summary>
        public bool UnderscoresToWhitespaces { get; set; } = false;

        /// <summary>
        /// Indicates whether the string should be URL-encoded.
        /// </summary>
        public bool UrlEncode { get; set; } = false;

        /// <summary>
        /// Indicates whether whitespace characters in the string should be replaced with hyphens.
        /// </summary>
        public bool WhitespacesToHyphens { get; set; } = false;

        /// <summary>
        /// Indicates whether whitespace characters in the string should be replaced with underscores.
        /// </summary>
        public bool WhitespacesToUnderscores { get; set; } = false;
    }
}
