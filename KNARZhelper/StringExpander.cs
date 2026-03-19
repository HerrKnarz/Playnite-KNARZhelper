using Microsoft.Win32;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace KNARZhelper
{
    public class StringExpander : ObservableObject
    {
        private readonly Guid _gogId = Guid.Parse("aebe8b7c-6dc3-4a66-af31-e7375c6b5e9e");
        private readonly Guid _gogOssId = Guid.Parse("03689811-3F33-4DFB-A121-2EE168FB9A5C");
        private readonly string _placeholderDropbox = "{DropboxFolder}";
        private readonly string _placeholderGameName = "{GameName}";
        private readonly string _placeholderGogId = "{GogId}";
        private readonly string _placeholderGogScreenshotDir = "{GogScreenshotDir}";
        private readonly string _placeholderOneDrive = "{OneDriveFolder}";
        private readonly string _placeholderRetroArchScreenshots = "{RetroArchScreenshotsDir}";
        private readonly string _placeholderRomName = "{RomName}";
        private readonly string _placeholderSteamAccountId = "{SteamAccountId}";
        private readonly string _placeholderSteamDir = "{SteamInstallDir}";
        private readonly string _placeholderSteamId = "{SteamId}";
        private readonly string _placeholderSteamScreenshotsDir = "{SteamScreenshotsDir}";
        private readonly string _placeholderUbisoftGameDir = "{UbisoftGameDir}";
        private readonly string _placeholderUbisoftInstallDir = "{UbisoftInstallDir}";
        private readonly string _placeholderUbisoftScreenshotsDir = "{UbisoftScreenshotsDir}";
        private readonly string _placeholderXboxGamebarScreenshotsDir = "{XboxGamebarScreenshotsDir}";

        private readonly string _typePlaceholderEnvVar = "ENV";
        private readonly string _typePlaceholderEnvVarLocal = "EnvVar";
        private readonly string _typePlaceholderFolderPath = "Folder path";
        private readonly string _typePlaceholderFolderPathLocal = "FolderPathLocal";
        private readonly string _typePlaceholderGameInfo = "Game info";
        private readonly string _typePlaceholderGameInfoLocal = "GameInfo";

        private string _dropBoxFolder = null;
        private string _gogScreenshotDir = null;
        private string _oneDriveFolder = null;
        private string _retroArchScreenshotsDir = null;
        private string _steamAccountId = null;
        private string _steamInstallDir = null;
        private string _steamScreenshotsDir = null;
        private bool _ubisoftDirsRead = false;
        private string _ubisoftGameDir = null;
        private string _ubisoftInstallDir = null;
        private string _ubisoftScreenshotsDir = null;
        private string _xboxGamebarScreenshotDir = null;

        public StringExpander(string localizationPrefix = "")
        {
            var tmpPlaceholders = new List<StringPlaceholder>
            {
                ////// Playnite variables //////

                new StringPlaceholder
                {
                    Placeholder = "{ImagePath}",
                    Type = _typePlaceholderFolderPath,
                    TypeLocalizationString = localizationPrefix + _typePlaceholderFolderPathLocal,
                    LocalizationPrefix = localizationPrefix,
                    Description = "Game ISO/ROM path if set",
                },
                new StringPlaceholder
                {
                    Placeholder = "{InstallDir}",
                    Type = _typePlaceholderFolderPath,
                    TypeLocalizationString = localizationPrefix + _typePlaceholderFolderPathLocal,
                    LocalizationPrefix = localizationPrefix,
                    Description = "Game installation directory",
                },
                new StringPlaceholder
                {
                    Placeholder = "{InstallDirName}",
                    Type = _typePlaceholderFolderPath,
                    TypeLocalizationString = localizationPrefix + _typePlaceholderFolderPathLocal,
                    LocalizationPrefix = localizationPrefix,
                    Description = "Name of installation folder",
                },
                new StringPlaceholder
                {
                    Placeholder = "{ImageName}",
                    Type = _typePlaceholderGameInfo,
                    TypeLocalizationString = localizationPrefix + _typePlaceholderGameInfoLocal,
                    LocalizationPrefix = localizationPrefix,
                    Description = "Game ISO/ROM file name",
                },
                new StringPlaceholder
                {
                    Placeholder = "{ImageNameNoExt}",
                    Type = _typePlaceholderGameInfo,
                    TypeLocalizationString = localizationPrefix + _typePlaceholderGameInfoLocal,
                    LocalizationPrefix = localizationPrefix,
                    Description = "Game ISO/ROM file name without extension",
                },
                new StringPlaceholder
                {
                    Placeholder = "{PlayniteDir}",
                    Type = _typePlaceholderFolderPath,
                    TypeLocalizationString = localizationPrefix + _typePlaceholderFolderPathLocal,
                    LocalizationPrefix = localizationPrefix,
                    Description = "Playnite's installation directory",
                },
                new StringPlaceholder
                {
                    Placeholder = "{Name}",
                    Type = _typePlaceholderGameInfo,
                    TypeLocalizationString = localizationPrefix + _typePlaceholderGameInfoLocal,
                    LocalizationPrefix = localizationPrefix,
                    Description = "Game name",
                },
                new StringPlaceholder
                {
                    Placeholder = "{Platform}",
                    Type = _typePlaceholderGameInfo,
                    TypeLocalizationString = localizationPrefix + _typePlaceholderGameInfoLocal,
                    LocalizationPrefix = localizationPrefix,
                    Description = "Game's platform",
                },
                new StringPlaceholder
                {
                    Placeholder = "{GameId}",
                    Type = _typePlaceholderGameInfo,
                    TypeLocalizationString = localizationPrefix + _typePlaceholderGameInfoLocal,
                    LocalizationPrefix = localizationPrefix,
                    Description = "Game's ID given by the library",
                },
                new StringPlaceholder
                {
                    Placeholder = "{DatabaseId}",
                    Type = _typePlaceholderGameInfo,
                    TypeLocalizationString = localizationPrefix + _typePlaceholderGameInfoLocal,
                    LocalizationPrefix = localizationPrefix,
                    Description = "Game's database ID",
                },
                new StringPlaceholder
                {
                    Placeholder = "{PluginId}",
                    Type = _typePlaceholderGameInfo,
                    TypeLocalizationString = localizationPrefix + _typePlaceholderGameInfoLocal,
                    LocalizationPrefix = localizationPrefix,
                    Description = "Game's library plugin ID",
                },
                new StringPlaceholder
                {
                    Placeholder = "{Version}",
                    Type = _typePlaceholderGameInfo,
                    TypeLocalizationString = localizationPrefix + _typePlaceholderGameInfoLocal,
                    LocalizationPrefix = localizationPrefix,
                    Description = "Game version",
                },
                new StringPlaceholder
                {
                    Placeholder = "{EmulatorDir}",
                    Type = _typePlaceholderFolderPath,
                    TypeLocalizationString = localizationPrefix + _typePlaceholderFolderPathLocal,
                    LocalizationPrefix = localizationPrefix,
                    Description = "Emulator's installation directory",
                },

                ////// Screenshot Utilities variables //////

                new StringPlaceholder
                {
                    Placeholder = _placeholderDropbox,
                    Type = _typePlaceholderFolderPath,
                    TypeLocalizationString = localizationPrefix + _typePlaceholderFolderPathLocal,
                    LocalizationPrefix = localizationPrefix,
                    Description = "Dropbox folder - Requires Dropbox to be installed",
                },
                new StringPlaceholder
                {
                    Placeholder = _placeholderGameName,
                    Type = _typePlaceholderGameInfo,
                    TypeLocalizationString = localizationPrefix + _typePlaceholderGameInfoLocal,
                    LocalizationPrefix = localizationPrefix,
                    Description = "Game name formatted based on the set check boxes"
                },
                new StringPlaceholder
                {
                    Placeholder = _placeholderGogId,
                    Type = _typePlaceholderGameInfo,
                    TypeLocalizationString = localizationPrefix + _typePlaceholderGameInfoLocal,
                    LocalizationPrefix = localizationPrefix,
                    Description = "Game's GOG ID if it's a GOG game"
                },
                new StringPlaceholder
                {
                    Placeholder = _placeholderGogScreenshotDir,
                    Type = _typePlaceholderFolderPath,
                    TypeLocalizationString = localizationPrefix + _typePlaceholderFolderPathLocal,
                    LocalizationPrefix = localizationPrefix,
                    Description = "GOG Galaxy screenshots folder"
                },
                new StringPlaceholder
                {
                    Placeholder = _placeholderOneDrive,
                    Type = _typePlaceholderFolderPath,
                    TypeLocalizationString = localizationPrefix + _typePlaceholderFolderPathLocal,
                    LocalizationPrefix = localizationPrefix,
                    Description = "OneDrive folder - Requires OneDrive to be installed",
                },
                new StringPlaceholder
                {
                    Placeholder = _placeholderRetroArchScreenshots,
                    Type = _typePlaceholderFolderPath,
                    TypeLocalizationString = localizationPrefix + _typePlaceholderFolderPathLocal,
                    LocalizationPrefix = localizationPrefix,
                    Description = "RetroArch screenshots folder - Requires RetroArch to be installed"
                },
                new StringPlaceholder
                {
                    Placeholder = _placeholderRomName,
                    Type = _typePlaceholderGameInfo,
                    TypeLocalizationString = localizationPrefix + _typePlaceholderGameInfoLocal,
                    LocalizationPrefix = localizationPrefix,
                    Description = "Name of the first ROM of the game without extension"
                },
                new StringPlaceholder
                {
                    Placeholder = _placeholderSteamAccountId,
                    Type = _typePlaceholderGameInfo,
                    TypeLocalizationString = localizationPrefix + _typePlaceholderGameInfoLocal,
                    LocalizationPrefix = localizationPrefix,
                    Description = "Steam account ID of the currently active Steam user"
                },
                new StringPlaceholder
                {
                    Placeholder = _placeholderSteamDir,
                    Type = _typePlaceholderFolderPath,
                    TypeLocalizationString = localizationPrefix + _typePlaceholderFolderPathLocal,
                    LocalizationPrefix = localizationPrefix,
                    Description = "Steam installation folder - Requires Steam to be installed on the system"
                },
                new StringPlaceholder
                {
                    Placeholder = _placeholderSteamId,
                    Type = _typePlaceholderGameInfo,
                    TypeLocalizationString = localizationPrefix + _typePlaceholderGameInfoLocal,
                    LocalizationPrefix = localizationPrefix,
                    Description = "Game's Steam ID if it's a Steam game or has a steam link"
                },
                new StringPlaceholder
                {
                    Placeholder = _placeholderSteamScreenshotsDir,
                    Type = _typePlaceholderFolderPath,
                    TypeLocalizationString = localizationPrefix + _typePlaceholderFolderPathLocal,
                    LocalizationPrefix = localizationPrefix,
                    Description = "Steam screenshots folder - Requires Steam to be installed"
                },
                new StringPlaceholder
                {
                    Placeholder = _placeholderUbisoftGameDir,
                    Type = _typePlaceholderFolderPath,
                    TypeLocalizationString = localizationPrefix + _typePlaceholderFolderPathLocal,
                    LocalizationPrefix = localizationPrefix,
                    Description = "Ubisoft games installation folder - Requires Ubisoft Connect to be installed"
                },
                new StringPlaceholder
                {
                    Placeholder = _placeholderUbisoftInstallDir,
                    Type = _typePlaceholderFolderPath,
                    TypeLocalizationString = localizationPrefix + _typePlaceholderFolderPathLocal,
                    LocalizationPrefix = localizationPrefix,
                    Description = "Ubisoft Connect installation folder - Requires Ubisoft Connect to be installed"
                },
                new StringPlaceholder
                {
                    Placeholder = _placeholderUbisoftScreenshotsDir,
                    Type = _typePlaceholderFolderPath,
                    TypeLocalizationString = localizationPrefix + _typePlaceholderFolderPathLocal,
                    LocalizationPrefix = localizationPrefix,
                    Description = "Ubisoft Connect screenshots folder - Requires Ubisoft Connect to be installed"
                },
                new StringPlaceholder
                {
                    Placeholder = _placeholderXboxGamebarScreenshotsDir,
                    Type = _typePlaceholderFolderPath,
                    TypeLocalizationString = localizationPrefix + _typePlaceholderFolderPathLocal,
                    LocalizationPrefix = localizationPrefix,
                    Description = "Xbox Game Bar screenshots folder"
                }
            };

            ////// Environment variables //////

            tmpPlaceholders.AddRange(Environment.GetEnvironmentVariables().Cast<DictionaryEntry>().Select(p => new StringPlaceholder
            {
                Placeholder = "{" + (string)p.Key + "}",
                Type = _typePlaceholderEnvVar,
                TypeLocalizationString = localizationPrefix + _typePlaceholderEnvVarLocal,
                LocalizationPrefix = localizationPrefix
            }).OrderBy(p => p.Placeholder));

            Placeholders.AddMissing(tmpPlaceholders.OrderBy(p => p.Type.Equals(_typePlaceholderEnvVar) ? "X" : p.Type).ThenBy(p => p.Placeholder));
        }

        public string DropBoxFolder
        {
            get
            {
                if (_dropBoxFolder == null)
                {
                    _dropBoxFolder = GetDropBoxFolder();
                }

                return _dropBoxFolder;
            }
        }

        public string GogScreenshotDir
        {
            get
            {
                if (_gogScreenshotDir == null)
                {
                    _gogScreenshotDir = GetGogScreenshotDir();
                }

                return _gogScreenshotDir;
            }
        }

        public string OneDriveFolder
        {
            get
            {
                if (_oneDriveFolder == null)
                {
                    _oneDriveFolder = GetOneDriveFolder();
                }

                return _oneDriveFolder;
            }
        }

        public ObservableCollection<StringPlaceholder> Placeholders { get; set; } = new ObservableCollection<StringPlaceholder>();

        public string RetroArchScreenshotsDir
        {
            get
            {
                if (_retroArchScreenshotsDir == null)
                {
                    _retroArchScreenshotsDir = GetRetroArchScreenshotDir();
                }

                return _retroArchScreenshotsDir;
            }
        }

        public string SteamAccountId
        {
            get
            {
                if (_steamAccountId == null)
                {
                    _steamAccountId = GetSteamAccountId();
                }

                return _steamAccountId;
            }
        }

        public string SteamInstallDir
        {
            get
            {
                if (_steamInstallDir == null)
                {
                    _steamInstallDir = GetSteamInstallDir();
                }

                return _steamInstallDir;
            }
        }

        public string SteamScreenshotsDir
        {
            get
            {
                if (_steamScreenshotsDir == null)
                {
                    _steamScreenshotsDir = GetSteamScreenshotDir();
                }

                return _steamScreenshotsDir;
            }
        }

        public string UbisoftGameDir
        {
            get
            {
                if (_ubisoftGameDir == null && !_ubisoftDirsRead)
                {
                    GetUbisoftDirs();
                }

                return _ubisoftGameDir;
            }
        }

        public string UbisoftInstallDir
        {
            get
            {
                if (_ubisoftInstallDir == null && !_ubisoftDirsRead)
                {
                    GetUbisoftDirs();
                }

                return _ubisoftInstallDir;
            }
        }

        public string UbisoftScreenshotsDir
        {
            get
            {
                if (_ubisoftScreenshotsDir == null && !_ubisoftDirsRead)
                {
                    GetUbisoftDirs();
                }

                return _ubisoftScreenshotsDir;
            }
        }

        public string XboxGamebarScreenshotDir
        {
            get
            {
                if (_xboxGamebarScreenshotDir == null)
                {
                    _xboxGamebarScreenshotDir = GetXboxGamebarScreenshotDir();
                }

                return _xboxGamebarScreenshotDir;
            }
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
        public string ReplaceAllPlaceholders(string str, Game game, string gameName = null)
        {
            if (string.IsNullOrEmpty(str) || !str.Contains('{'))
            {
                return str;
            }

            if (game == null)
            {
                game = new Game();
            }

            if (string.IsNullOrEmpty(gameName))
            {
                gameName = game.Name;
            }

            str = API.Instance.ExpandGameVariables(game, str);

            if (ReplaceSteamAccountIdPlaceholder(ref str)
                && ReplaceSteamDirPlaceholder(ref str)
                && ReplaceSteamIdPlaceholder(ref str, game)
                && ReplaceSteamScreenshotDirPlaceholder(ref str)
                && ReplaceGameNamePlaceholder(ref str, gameName)
                && ReplaceGogIdPlaceholder(ref str, game)
                && ReplaceGogScreenshotDirPlaceholder(ref str)
                && ReplaceDropBoxPlaceholder(ref str)
                && ReplaceOneDrivePlaceholder(ref str)
                && ReplaceRetroArchPlaceholder(ref str)
                && ReplaceRomNamePlaceholder(ref str, game)
                && ReplaceUbisoftGameDirPlaceholder(ref str)
                && ReplaceUbisoftInstallDirPlaceholder(ref str)
                && ReplaceUbisoftScreenshotsDirPlaceholder(ref str)
                && ReplaceXboxGamebarScreenshotDirPlaceholder(ref str))
            {
                // Replace environment variables last to avoid conflicts with other placeholders
                str = ReplaceEnvironmentVariables(str);

                return str;
            }

            return string.Empty;
        }

        /// <summary>
        /// Replaces the Dropbox folder placeholder in the specified string with the actual Dropbox
        /// folder path, if available.
        /// </summary>
        /// <remarks>
        /// This method searches for a Dropbox folder placeholder in the input string and attempts
        /// to resolve it using the Dropbox configuration file located in the user's AppData directory.
        /// </remarks>
        /// <param name="str">
        /// A reference to the string in which the Dropbox folder placeholder will be replaced. The
        /// string will be modified if the placeholder is found and resolved.
        /// </param>
        /// <returns>
        /// true if the placeholder was found and replaced or wasn't present in the string;
        /// otherwise, false.
        /// </returns>
        public bool ReplaceDropBoxPlaceholder(ref string str)
            => ReplaceSinglePlaceholder(ref str, _placeholderDropbox, DropBoxFolder);

        /// <summary>
        /// Replaces placeholders in the string with the values of corresponding environment variables.
        /// </summary>
        /// <remarks>
        /// Placeholders must match the names of environment variables exactly, including case
        /// sensitivity. If an environment variable is not set, its placeholder will just be removed.
        /// </remarks>
        /// <param name="str">
        /// The input string containing placeholders in the format "{VARIABLE_NAME}" to be replaced
        /// with environment variable values.
        /// </param>
        /// <returns>
        /// A string with all recognized environment variable placeholders replaced by their values.
        /// Returns the original string if no placeholders are found or if the input is null or empty.
        /// </returns>
        public string ReplaceEnvironmentVariables(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return str;
            }

            foreach (DictionaryEntry envVar in Environment.GetEnvironmentVariables())
            {
                var placeholder = "{" + (string)envVar.Key + "}";

                if (str.Contains(placeholder))
                {
                    str = str.Replace(placeholder, (string)envVar.Value);
                }
            }

            return str;
        }

        /// <summary>
        /// Replaces the game name placeholder in the specified string with the provided game name.
        /// </summary>
        /// <param name="str">
        /// A reference to the string in which the game name placeholder will be replaced. The
        /// string will be modified if the placeholder is found and resolved.
        /// </param>
        /// <param name="gameName">The game name to substitute for the placeholder.</param>
        /// <returns>
        /// true if the placeholder was found and replaced or wasn't present in the string;
        /// otherwise, false.
        /// </returns>
        public bool ReplaceGameNamePlaceholder(ref string str, string gameName)
            => ReplaceSinglePlaceholder(ref str, _placeholderGameName, gameName);

        /// <summary>
        /// Replaces the GOG Id placeholder in the specified string with the GOG game identifier if applicable.
        /// </summary>
        /// <param name="str">
        /// A reference to the string in which the GOG Id placeholder will be replaced. The string
        /// will be modified if the placeholder is found and resolved.
        /// </param>
        /// <param name="game">The game instance providing the GOG Id and plugin information.</param>
        /// <returns>
        /// true if the placeholder was found and replaced or wasn't present in the string;
        /// otherwise, false.
        /// </returns>
        public bool ReplaceGogIdPlaceholder(ref string str, Game game)
        {
            var gogGameId = game.PluginId.IsOneOf(_gogId, _gogOssId) ? game.GameId : string.Empty;

            return ReplaceSinglePlaceholder(ref str, _placeholderGogId, gogGameId);
        }

        /// <summary>
        /// Replaces the GOG screenshot dir placeholder in the specified string with the actual GOG
        /// screenshot dir.
        /// </summary>
        /// <param name="str">
        /// A reference to the string in which the GOG screenshot dir placeholder will be replaced.
        /// The string will be modified if the placeholder is found and resolved.
        /// </param>
        /// <returns>
        /// true if the placeholder was found and replaced or wasn't present in the string;
        /// otherwise, false.
        /// </returns>
        public bool ReplaceGogScreenshotDirPlaceholder(ref string str)
            => ReplaceSinglePlaceholder(ref str, _placeholderGogScreenshotDir, GogScreenshotDir);

        /// <summary>
        /// Replaces the OneDrive folder placeholder in the specified string with the actual
        /// OneDrive folder path, if available.
        /// </summary>
        /// <remarks>
        /// This method searches for a OneDrive folder placeholder in the input string and attempts
        /// to resolve it using the OneDrive registry key.
        /// </remarks>
        /// <param name="str">
        /// A reference to the string in which the OneDrive folder placeholder will be replaced. The
        /// string will be modified if the placeholder is found and resolved.
        /// </param>
        /// <returns>
        /// true if the placeholder was found and replaced or wasn't present in the string;
        /// otherwise, false.
        /// </returns>
        public bool ReplaceOneDrivePlaceholder(ref string str)
            => ReplaceSinglePlaceholder(ref str, _placeholderOneDrive, OneDriveFolder);

        /// <summary>
        /// Replaces the RetroArch screenshot folder placeholder in the specified string with the
        /// actual directory path, if available.
        /// </summary>
        /// <param name="str">
        /// A reference to the string in which the RetroArch screenshot folder placeholder will be
        /// replaced. The string will be modified if the placeholder is found and resolved.
        /// </param>
        /// <returns>
        /// true if the placeholder was found and replaced or wasn't present in the string;
        /// otherwise, false.
        /// </returns>
        public bool ReplaceRetroArchPlaceholder(ref string str)
            => ReplaceSinglePlaceholder(ref str, _placeholderRetroArchScreenshots, RetroArchScreenshotsDir);

        /// <summary>
        /// Replaces the ROM name placeholder in the specified string with the file name of the
        /// first ROM associated with the given game, if available.
        /// </summary>
        /// <param name="str">
        /// A reference to the string in which the ROM name placeholder will be replaced. The string
        /// will be modified if the placeholder is found and resolved.
        /// </param>
        /// <param name="game">
        /// The game whose ROM file name is used to replace the placeholder. Must not be null.
        /// </param>
        /// <returns>
        /// true if the placeholder was found and replaced or wasn't present in the string;
        /// otherwise, false.
        /// </returns>
        public bool ReplaceRomNamePlaceholder(ref string str, Game game)
        {
            var romName = game.IsInstalled && (game.Roms?.Any() ?? false) ? Path.GetFileNameWithoutExtension(game.Roms[0].Path) : string.Empty;

            return ReplaceSinglePlaceholder(ref str, _placeholderRomName, romName);
        }

        /// <summary>
        /// Replaces the Steam account ID placeholder in the specified string with the actual Steam
        /// account ID.
        /// </summary>
        /// <param name="str">
        /// A reference to the string in which the Steam account ID placeholder will be replaced.
        /// The string will be modified if the placeholder is found and resolved.
        /// </param>
        /// <returns>
        /// true if the placeholder was found and replaced or wasn't present in the string;
        /// otherwise, false.
        /// </returns>
        public bool ReplaceSteamAccountIdPlaceholder(ref string str)
            => ReplaceSinglePlaceholder(ref str, _placeholderSteamAccountId, SteamAccountId);

        /// <summary>
        /// Replaces the Steam folder placeholder in the specified string with the actual Steam
        /// folder path, if available.
        /// </summary>
        /// <remarks>
        /// This method searches for a Steam folder placeholder in the input string and attempts to
        /// resolve it using the Steam registry key.
        /// </remarks>
        /// <param name="str">
        /// A reference to the string in which the Steam folder placeholder will be replaced. The
        /// string will be modified if the placeholder is found and resolved.
        /// </param>
        /// <returns>
        /// true if the placeholder was found and replaced or wasn't present in the string;
        /// otherwise, false.
        /// </returns>
        public bool ReplaceSteamDirPlaceholder(ref string str)
            => ReplaceSinglePlaceholder(ref str, _placeholderSteamDir, SteamInstallDir);

        /// <summary>
        /// Replaces the Steam ID placeholder in the specified string with the Steam ID associated
        /// with the given game.
        /// </summary>
        /// <param name="str">
        /// A reference to the string in which the Steam ID placeholder will be replaced. The string
        /// will be modified if the placeholder is found and resolved.
        /// </param>
        /// <param name="game">The game instance used to retrieve the Steam ID for replacement.</param>
        /// <returns>
        /// true if the placeholder was found and replaced or wasn't present in the string;
        /// otherwise, false.
        /// </returns>
        public bool ReplaceSteamIdPlaceholder(ref string str, Game game)
        {
            var steamGameId = SteamHelper.GetSteamId(game);

            return ReplaceSinglePlaceholder(ref str, _placeholderSteamId, steamGameId);
        }

        /// <summary>
        /// Replaces the Steam screenshot folder placeholder in the specified string with the Steam
        /// ID associated with the given game.
        /// </summary>
        /// <param name="str">
        /// A reference to the string in which the Steam screenshot folder placeholder will be
        /// replaced. The string will be modified if the placeholder is found and resolved.
        /// </param>
        /// <returns>
        /// true if the placeholder was found and replaced or wasn't present in the string;
        /// otherwise, false.
        /// </returns>
        public bool ReplaceSteamScreenshotDirPlaceholder(ref string str)
            => ReplaceSinglePlaceholder(ref str, _placeholderSteamScreenshotsDir, SteamScreenshotsDir);

        /// <summary>
        /// Replaces the Ubisoft game dir placeholder in the specified string with the actual
        /// Ubisoft game dir.
        /// </summary>
        /// <remarks>
        /// The directory is retrieved from the Ubisoft Game Launcher settings file. If the file
        /// cannot be read or does not contain the necessary information, the placeholder will not
        /// be replaced and the method will return false.
        /// </remarks>
        /// <param name="str">
        /// A reference to the string in which the Ubisoft game dir placeholder will be replaced.
        /// The string will be modified if the placeholder is found and resolved.
        /// </param>
        /// <returns>
        /// true if the placeholder was found and replaced or wasn't present in the string;
        /// otherwise, false.
        /// </returns>
        public bool ReplaceUbisoftGameDirPlaceholder(ref string str)
            => ReplaceSinglePlaceholder(ref str, _placeholderUbisoftGameDir, UbisoftGameDir);

        /// <summary>
        /// Replaces the Ubisoft install dir placeholder in the specified string with the Ubisoft
        /// install dir associated with the given game.
        /// </summary>
        /// <remarks>
        /// The directory is retrieved from the Ubisoft Game Launcher settings file. If the file
        /// cannot be read or does not contain the necessary information, the placeholder will not
        /// be replaced and the method will return false.
        /// </remarks>
        /// <param name="str">
        /// A reference to the string in which the Ubisoft install dir placeholder will be replaced.
        /// The string will be modified if the placeholder is found and resolved.
        /// </param>
        /// <returns>
        /// true if the placeholder was found and replaced or wasn't present in the string;
        /// otherwise, false.
        /// </returns>
        public bool ReplaceUbisoftInstallDirPlaceholder(ref string str)
            => ReplaceSinglePlaceholder(ref str, _placeholderUbisoftInstallDir, UbisoftInstallDir);

        /// <summary>
        /// Replaces the Ubisoft screenshot dir placeholder in the specified string with the Ubisoft
        /// screenshot dir associated with the given game.
        /// </summary>
        /// <remarks>
        /// The directory is retrieved from the Ubisoft Game Launcher settings file. If the file
        /// cannot be read or does not contain the necessary information, the placeholder will not
        /// be replaced and the method will return false.
        /// </remarks>
        /// <param name="str">
        /// A reference to the string in which the Ubisoft screenshot dir placeholder will be
        /// replaced. The string will be modified if the placeholder is found and resolved.
        /// </param>
        /// <returns>
        /// true if the placeholder was found and replaced or wasn't present in the string;
        /// otherwise, false.
        /// </returns>
        public bool ReplaceUbisoftScreenshotsDirPlaceholder(ref string str)
            => ReplaceSinglePlaceholder(ref str, _placeholderUbisoftScreenshotsDir, UbisoftScreenshotsDir);

        /// <summary>
        /// Replaces the Xbox gamebar screenshot dir placeholder in the specified string with the
        /// actual Xbox gamebar screenshot dir.
        /// </summary>
        /// <param name="str">
        /// A reference to the string in which the Xbox gamebar screenshot dir placeholder will be
        /// replaced. The string will be modified if the placeholder is found and resolved.
        /// </param>
        /// <returns>
        /// true if the placeholder was found and replaced or wasn't present in the string;
        /// otherwise, false.
        /// </returns>
        public bool ReplaceXboxGamebarScreenshotDirPlaceholder(ref string str)
            => ReplaceSinglePlaceholder(ref str, _placeholderXboxGamebarScreenshotsDir, XboxGamebarScreenshotDir);

        public void TestExpansions(Game game)
        {
            ResetCache();

            var resultString = string.Empty;

            resultString += "Dropbox: " + ReplaceAllPlaceholders("{DropboxFolder}", game);
            resultString += "\nEnvironment variables: " + ReplaceAllPlaceholders("{ProgramFiles}, {ProgramFiles(x86)}, {UserProfile}, {AppData}, {LocalAppData}, {Temp}", game);
            resultString += "\nGame name: " + ReplaceAllPlaceholders("{GameName}", game);
            resultString += "\nGOG ID: " + ReplaceAllPlaceholders("{GogId}", game);
            resultString += "\nGOG Screenshot Dir: " + ReplaceAllPlaceholders("{GogScreenshotDir}", game);
            resultString += "\nOneDrive: " + ReplaceAllPlaceholders("{OneDriveFolder}", game);
            resultString += "\nPlaynite variables: " + ReplaceAllPlaceholders("{Name}, {InstallDir}, {Platform}, {GameId}", game);
            resultString += "\nRetroArch: " + ReplaceAllPlaceholders("{RetroArchScreenshotsDir}", game);
            resultString += "\nROM name: " + ReplaceAllPlaceholders("{RomName}", game);
            resultString += "\nSteam AccountId: " + ReplaceAllPlaceholders("{SteamAccountId}", game);
            resultString += "\nSteam Dir: " + ReplaceAllPlaceholders("{SteamInstallDir}", game);
            resultString += "\nSteam ID: " + ReplaceAllPlaceholders("{SteamId}", game);
            resultString += "\nSteam Screenshots: " + ReplaceAllPlaceholders("{SteamScreenshotsDir}", game);
            resultString += "\nUbisoft Game Dir: " + ReplaceAllPlaceholders("{UbisoftGameDir}", game);
            resultString += "\nUbisoft Install Dir: " + ReplaceAllPlaceholders("{UbisoftInstallDir}", game);
            resultString += "\nUbisoft Screenshots Dir: " + ReplaceAllPlaceholders("{UbisoftScreenshotsDir}", game);
            resultString += "\nXbox Gamebar Screenshot Dir: " + ReplaceAllPlaceholders("{XboxGamebarScreenshotsDir}", game);

            API.Instance.Dialogs.ShowSelectableString("", "Placeholder Test", resultString);
        }

        private string GetDropBoxFolder()
        {
            try
            {
                var DropboxInfoFile = Path.Combine(Environment.GetEnvironmentVariable("AppData"), "..", "Local", "Dropbox", "info.json");

                if (File.Exists(DropboxInfoFile))
                {
                    var DropboxInfo = Serialization.FromJsonFile<dynamic>(DropboxInfoFile);
                    return DropboxInfo["personal"]["path"].Value;
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while reading Dropbox folder");

                return string.Empty;
            }
        }

        private string GetGogScreenshotDir()
        {
            var screenshotDir = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Documents", "GOG Galaxy", "Screenshots"));

            return screenshotDir.Exists ? screenshotDir.FullName : string.Empty;
        }

        private string GetOneDriveFolder()
        {
            try
            {
                var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\OneDrive");

                return key?.GetValueNames().Contains("UserFolder") == true
                    ? key.GetValue("UserFolder").ToString().Replace('/', '\\')
                    : string.Empty;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while reading OneDrive folder");
                return string.Empty;
            }
        }

        private string GetRetroArchScreenshotDir()
        {
            try
            {
                var emulator = API.Instance.Database.Emulators.FirstOrDefault(x => x.Name.Contains("RetroArch", StringComparison.OrdinalIgnoreCase));

                if (emulator == null)
                {
                    return string.Empty;
                }

                var retroArchConfig = new FileInfo(Path.Combine(emulator.InstallDir, "retroarch.cfg"));

                if (!retroArchConfig.Exists)
                {
                    return string.Empty;
                }

                var screenshotDirLine = File.ReadAllLines(retroArchConfig.FullName).FirstOrDefault(x => x.Contains("screenshot_directory", StringComparison.OrdinalIgnoreCase));

                if (screenshotDirLine == default)
                {
                    return string.Empty;
                }

                var screenshotDir = screenshotDirLine.Replace("screenshot_directory = ", string.Empty)
                                                            .Replace("\"", string.Empty)
                                                            .Trim();

                if (screenshotDir.StartsWith(":"))
                {
                    screenshotDir = screenshotDir.Replace(":", emulator.InstallDir);
                }

                return screenshotDir;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while reading RetroArch folder");

                return string.Empty;
            }
        }

        private string GetSteamAccountId()
        {
            try
            {
                var steamUserDir = new DirectoryInfo(Path.Combine(SteamInstallDir, "userdata"));

                if (!steamUserDir.Exists)
                {
                    return string.Empty;
                }

                var steamUserIdDir = steamUserDir.GetDirectories()
                    .OrderBy(d => d.GetDirectories("config")
                    .FirstOrDefault().LastWriteTime).First();

                return steamUserIdDir == null ? string.Empty : steamUserIdDir.Name;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while reading Steam userdata folder");

                return string.Empty;
            }
        }

        private string GetSteamInstallDir()
        {
            try
            {
                var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
                return key?.GetValueNames().Contains("SteamPath") == true
                    ? key.GetValue("SteamPath").ToString().Replace('/', '\\')
                    : string.Empty;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while reading Steam folder");
                return string.Empty;
            }
        }

        private string GetSteamScreenshotDir()
        {
            var screenshotDir = new DirectoryInfo(Path.Combine(SteamInstallDir, "userdata", SteamAccountId, "760", "remote"));

            return screenshotDir.Exists ? screenshotDir.FullName : string.Empty;
        }

        private void GetUbisoftDirs()
        {
            var configPath = Path.Combine(Environment.GetEnvironmentVariable("AppData"), "..", "Local", "Ubisoft Game Launcher", "settings.yaml");
            _ubisoftDirsRead = true;

            if (!File.Exists(configPath))
            {
                return;
            }

            dynamic SettingsData = Serialization.FromYamlFile<dynamic>(configPath);

            _ubisoftInstallDir = ((string)SettingsData["misc"]["installer_cache_path"]).Replace("cache/installers/", string.Empty).Replace('/', Path.DirectorySeparatorChar);
            _ubisoftGameDir = ((string)SettingsData["misc"]["game_installation_path"]).Replace('/', Path.DirectorySeparatorChar);
            _ubisoftScreenshotsDir = ((string)SettingsData["misc"]["screenshot_root_path"]).Replace('/', Path.DirectorySeparatorChar);
        }

        private string GetXboxGamebarScreenshotDir()
        {
            var screenshotDir = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Videos", "Captures"));

            return screenshotDir.Exists ? screenshotDir.FullName : string.Empty;
        }

        private bool ReplaceSinglePlaceholder(ref string str, string placeholder, string value)
        {
            if (string.IsNullOrEmpty(str) || !str.Contains(placeholder))
            {
                return true;
            }

            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            str = str.Replace(placeholder, value);
            return true;
        }

        private void ResetCache()
        {
            _dropBoxFolder = null;
            _gogScreenshotDir = null;
            _oneDriveFolder = null;
            _retroArchScreenshotsDir = null;
            _steamAccountId = null;
            _steamInstallDir = null;
            _steamScreenshotsDir = null;
            _ubisoftGameDir = null;
            _ubisoftInstallDir = null;
            _ubisoftScreenshotsDir = null;
            _xboxGamebarScreenshotDir = null;
            _ubisoftDirsRead = false;
        }
    }

    public class StringPlaceholder : ObservableObject
    {
        public string Description { get; set; }
        public string DescriptionLocalizationString => LocalizationPrefix + Placeholder.Replace("{", string.Empty).Replace("}", string.Empty) + "Description";
        public string LocalizationPrefix { get; set; }
        public string Placeholder { get; set; }
        public string Type { get; set; }
        public string TypeLocalizationString { get; set; }
    }
}
