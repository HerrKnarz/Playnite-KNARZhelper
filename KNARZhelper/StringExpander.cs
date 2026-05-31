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
        private readonly string _localizationPrefix;
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
        private ObservableCollection<StringPlaceholder> _placeholders;
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
            _localizationPrefix = localizationPrefix;
        }

        /// <summary>
        /// Returns the Dropbox folder using the Dropbox configuration file located in the user's
        /// AppData directory.
        /// </summary>
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

        /// <summary>
        /// Returns the GOG screenshot directory path based on the standard location in the user's
        /// Documents folder.
        /// </summary>
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

        /// <summary>
        /// Returns the OneDrive folder path by reading the OneDrive registry key. If the key cannot
        /// be read or does not exist, an empty string is returned.
        /// </summary>
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

        public ObservableCollection<StringPlaceholder> Placeholders
        {
            get
            {
                if (_placeholders == null)
                {
                    _placeholders = new ObservableCollection<StringPlaceholder>();
                    PopulatePlaceholders();
                }

                return _placeholders;
            }
        }

        /// <summary>
        /// Returns the RetroArch screenshots directory by reading the "screenshot_directory"
        /// setting from the RetroArch configuration file. If the configuration file cannot be read
        /// or does not contain the necessary information, an empty string is returned. To locate
        /// the RetroArch installation, the method checks the list of emulators in the Playnite
        /// database for one with "RetroArch" in its name and reads the configuration file from that
        /// location. If no such emulator is found, an empty string is returned.
        /// </summary>
        public string RetroArchScreenshotsDir
        {
            get
            {
                if (_retroArchScreenshotsDir == null)
                {
                    _retroArchScreenshotsDir = GetRetroArchScreenshotsDir();
                }

                return _retroArchScreenshotsDir;
            }
        }

        /// <summary>
        /// Returns the Steam account ID of the currently active Steam user by reading the Steam
        /// userdata directory. The method locates the Steam installation directory using the
        /// registry, then looks for the "userdata" folder within it. It retrieves the list of
        /// subdirectories in "userdata", which correspond to different Steam accounts, and selects
        /// the one with the most recent "config" folder modification date as the active account. If
        /// any step of this process fails (e.g., if the Steam installation cannot be found, if the
        /// userdata directory does not exist, or if there are no valid user directories), an empty
        /// string is returned.
        /// </summary>
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

        /// <summary>
        /// Returns the Steam installation directory by reading the "SteamPath" value from the Steam
        /// registry key. If the registry key cannot be read or does not exist, an empty string is returned.
        /// </summary>
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

        /// <summary>
        /// Returns the Steam screenshots directory path by combining the Steam installation
        /// directory, the "userdata" folder, the active Steam account ID, and the standard path to
        /// the screenshots folder within a Steam user's data. If any of the required components
        /// (Steam installation directory, account ID) cannot be retrieved, or if the resulting
        /// screenshots directory does not exist, an empty string is returned.
        /// </summary>
        /// <param name="str">
        /// A reference to the string in which the Steam screenshot folder placeholder will be
        /// replaced. The string will be modified if the placeholder is found and resolved.
        /// </param>
        /// <returns>
        /// true if the placeholder was found and replaced or wasn't present in the string;
        /// otherwise, false.
        /// </returns>
        public string SteamScreenshotsDir
        {
            get
            {
                if (_steamScreenshotsDir == null)
                {
                    _steamScreenshotsDir = GetSteamScreenshotsDir();
                }

                return _steamScreenshotsDir;
            }
        }

        /// <summary>
        /// Returns the Ubisoft game installation directory by reading the Ubisoft Game Launcher
        /// settings file located in the user's AppData directory. The method retrieves the
        /// "game_installation_path" value from the settings file, which specifies the default
        /// installation directory for Ubisoft games. If the settings file cannot be read or does
        /// not contain the necessary information, an empty string is returned.
        /// </summary>
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

        /// <summary>
        /// Returns the Ubisoft game installation directory by reading the Ubisoft Game Launcher
        /// settings file located in the user's AppData directory. The method retrieves the
        /// "installer_cache_path" value from the settings file, which specifies the installation
        /// directory for the Ubisoft Connect client itself. If the settings file cannot be read or
        /// does not contain the necessary information, an empty string is returned.
        /// </summary>
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

        /// <summary>
        /// Returns the Ubisoft game installation directory by reading the Ubisoft Game Launcher
        /// settings file located in the user's AppData directory. The method retrieves the
        /// "screenshot_root_path" value from the settings file, which specifies the root directory
        /// for Ubisoft Connect screenshots. If the settings file cannot be read or does not contain
        /// the necessary information, an empty string is returned.
        /// </summary>
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

        /// <summary>
        /// Returns the Xbox Game Bar screenshots directory path, which is typically located in the
        /// "Captures" folder within the "Videos" library of the user's profile. If the directory
        /// does not exist, an empty string is returned.
        /// </summary>
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

            foreach (var placeholder in Placeholders.Where(p => !p.IsPlayniteVar && str.Contains(p.Placeholder)))
            {
                if (placeholder.GameDependent)
                {
                    if (placeholder.Placeholder == _placeholderGameName)
                    {
                        ReplaceSinglePlaceholder(ref str, placeholder.Placeholder, gameName);
                    }
                    else if (!ReplaceSinglePlaceholder(ref str, placeholder.Placeholder, placeholder.ResultFunc(game)))
                    {
                        return string.Empty;
                    }
                }
                else if (!ReplaceSinglePlaceholder(ref str, placeholder.Placeholder, placeholder.Result))
                {
                    return string.Empty;
                }
            }

            return str.Contains("{") ? string.Empty : str;
        }

        public void ResetCache() => PopulatePlaceholders();

        public void TestExpansions(Game game, bool showResult = false)
        {
            Placeholders.Where(p => p.GameDependent).ForEach(placeholder =>
            {
                placeholder.Result = placeholder.IsPlayniteVar
                    ? API.Instance.ExpandGameVariables(game, placeholder.Placeholder)
                    : placeholder.Placeholder == _placeholderGameName ? game.Name : placeholder.ResultFunc(game);
            });

            if (showResult)
            {
                var resultString = string.Join("\n", Placeholders.Select(p => $"{p.Placeholder}: {p.Result}"));

                API.Instance.Dialogs.ShowSelectableString("", "Placeholder Test", resultString);
            }
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

        private string GetGogId(Game game) => game.PluginId.IsOneOf(_gogId, _gogOssId) ? game.GameId : string.Empty;

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

        private string GetRetroArchScreenshotsDir()
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

        private string GetRomName(Game game)
                                    => game.IsInstalled && (game.Roms?.Any() ?? false) ? Path.GetFileNameWithoutExtension(game.Roms[0].Path) : string.Empty;

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

        private string GetSteamScreenshotsDir()
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
            try
            {
                var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders");

                if (key?.GetValueNames().Contains("{EDC0FE71-98D8-4F4A-B920-C8DC133CB165}") == true)
                {
                    return key.GetValue("{EDC0FE71-98D8-4F4A-B920-C8DC133CB165}").ToString().Replace('/', '\\');
                }

                var screenshotDir = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Videos", "Captures"));

                return screenshotDir.Exists ? screenshotDir.FullName : string.Empty;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while reading XBox Game Bar folder");
                return string.Empty;
            }
        }

        private void PopulatePlaceholders()
        {
            Placeholders.Clear();

            var game = new Game();

            GetUbisoftDirs();

            var tmpPlaceholders = new List<StringPlaceholder>
            {
                ////// Playnite variables //////

                new StringPlaceholder
                {
                    Placeholder = "{ImagePath}",
                    Type = _typePlaceholderFolderPath,
                    TypeLocalizationString = _localizationPrefix + _typePlaceholderFolderPathLocal,
                    LocalizationPrefix = _localizationPrefix,
                    Description = "Game ISO/ROM path if set",
                    GameDependent = true,
                    IsPlayniteVar = true
                },
                new StringPlaceholder
                {
                    Placeholder = "{InstallDir}",
                    Type = _typePlaceholderFolderPath,
                    TypeLocalizationString = _localizationPrefix + _typePlaceholderFolderPathLocal,
                    LocalizationPrefix = _localizationPrefix,
                    Description = "Game installation directory",
                    GameDependent = true,
                    IsPlayniteVar = true
                },
                new StringPlaceholder
                {
                    Placeholder = "{InstallDirName}",
                    Type = _typePlaceholderFolderPath,
                    TypeLocalizationString = _localizationPrefix + _typePlaceholderFolderPathLocal,
                    LocalizationPrefix = _localizationPrefix,
                    Description = "Name of installation folder",
                    GameDependent = true,
                    IsPlayniteVar = true
                },
                new StringPlaceholder
                {
                    Placeholder = "{ImageName}",
                    Type = _typePlaceholderGameInfo,
                    TypeLocalizationString = _localizationPrefix + _typePlaceholderGameInfoLocal,
                    LocalizationPrefix = _localizationPrefix,
                    Description = "Game ISO/ROM file name",
                    GameDependent = true,
                    IsPlayniteVar = true
                },
                new StringPlaceholder
                {
                    Placeholder = "{ImageNameNoExt}",
                    Type = _typePlaceholderGameInfo,
                    TypeLocalizationString = _localizationPrefix + _typePlaceholderGameInfoLocal,
                    LocalizationPrefix = _localizationPrefix,
                    Description = "Game ISO/ROM file name without extension",
                    GameDependent = true,
                    IsPlayniteVar = true
                },
                new StringPlaceholder
                {
                    Placeholder = "{PlayniteDir}",
                    Type = _typePlaceholderFolderPath,
                    TypeLocalizationString = _localizationPrefix + _typePlaceholderFolderPathLocal,
                    LocalizationPrefix = _localizationPrefix,
                    Description = "Playnite's installation directory",
                    Result = API.Instance.ExpandGameVariables(game, "{PlayniteDir}"),
                    IsPlayniteVar = true
                },
                new StringPlaceholder
                {
                    Placeholder = "{Name}",
                    Type = _typePlaceholderGameInfo,
                    TypeLocalizationString = _localizationPrefix + _typePlaceholderGameInfoLocal,
                    LocalizationPrefix = _localizationPrefix,
                    Description = "Game name",
                    GameDependent = true,
                    IsPlayniteVar = true
                },
                new StringPlaceholder
                {
                    Placeholder = "{Platform}",
                    Type = _typePlaceholderGameInfo,
                    TypeLocalizationString = _localizationPrefix + _typePlaceholderGameInfoLocal,
                    LocalizationPrefix = _localizationPrefix,
                    Description = "Game's platform",
                    GameDependent = true,
                    IsPlayniteVar = true
                },
                new StringPlaceholder
                {
                    Placeholder = "{GameId}",
                    Type = _typePlaceholderGameInfo,
                    TypeLocalizationString = _localizationPrefix + _typePlaceholderGameInfoLocal,
                    LocalizationPrefix = _localizationPrefix,
                    Description = "Game's ID given by the library",
                    GameDependent = true,
                    IsPlayniteVar = true
                },
                new StringPlaceholder
                {
                    Placeholder = "{DatabaseId}",
                    Type = _typePlaceholderGameInfo,
                    TypeLocalizationString = _localizationPrefix + _typePlaceholderGameInfoLocal,
                    LocalizationPrefix = _localizationPrefix,
                    Description = "Game's database ID",
                    GameDependent = true,
                    IsPlayniteVar = true
                },
                new StringPlaceholder
                {
                    Placeholder = "{PluginId}",
                    Type = _typePlaceholderGameInfo,
                    TypeLocalizationString = _localizationPrefix + _typePlaceholderGameInfoLocal,
                    LocalizationPrefix = _localizationPrefix,
                    Description = "Game's library plugin ID",
                    GameDependent = true,
                    IsPlayniteVar = true
                },
                new StringPlaceholder
                {
                    Placeholder = "{Version}",
                    Type = _typePlaceholderGameInfo,
                    TypeLocalizationString = _localizationPrefix + _typePlaceholderGameInfoLocal,
                    LocalizationPrefix = _localizationPrefix,
                    Description = "Game version",
                    GameDependent = true,
                    IsPlayniteVar = true
                },
                new StringPlaceholder
                {
                    Placeholder = "{EmulatorDir}",
                    Type = _typePlaceholderFolderPath,
                    TypeLocalizationString = _localizationPrefix + _typePlaceholderFolderPathLocal,
                    LocalizationPrefix = _localizationPrefix,
                    Description = "Emulator's installation directory",
                    GameDependent = true,
                    IsPlayniteVar = true
                },

                ////// Screenshot Utilities variables //////

                new StringPlaceholder
                {
                    Placeholder = _placeholderDropbox,
                    Type = _typePlaceholderFolderPath,
                    TypeLocalizationString = _localizationPrefix + _typePlaceholderFolderPathLocal,
                    LocalizationPrefix = _localizationPrefix,
                    Description = "Dropbox folder - Requires Dropbox to be installed",
                    Result = DropBoxFolder
                },
                new StringPlaceholder
                {
                    Placeholder = _placeholderGameName,
                    Type = _typePlaceholderGameInfo,
                    TypeLocalizationString = _localizationPrefix + _typePlaceholderGameInfoLocal,
                    LocalizationPrefix = _localizationPrefix,
                    Description = "Game name formatted based on the set check boxes",
                    GameDependent = true
                },
                new StringPlaceholder
                {
                    Placeholder = _placeholderGogId,
                    Type = _typePlaceholderGameInfo,
                    TypeLocalizationString = _localizationPrefix + _typePlaceholderGameInfoLocal,
                    LocalizationPrefix = _localizationPrefix,
                    Description = "Game's GOG ID if it's a GOG game",
                    GameDependent = true,
                    ResultFunc = GetGogId
                },
                new StringPlaceholder
                {
                    Placeholder = _placeholderGogScreenshotDir,
                    Type = _typePlaceholderFolderPath,
                    TypeLocalizationString = _localizationPrefix + _typePlaceholderFolderPathLocal,
                    LocalizationPrefix = _localizationPrefix,
                    Description = "GOG Galaxy screenshots folder",
                    Result = GogScreenshotDir
                },
                new StringPlaceholder
                {
                    Placeholder = _placeholderOneDrive,
                    Type = _typePlaceholderFolderPath,
                    TypeLocalizationString = _localizationPrefix + _typePlaceholderFolderPathLocal,
                    LocalizationPrefix = _localizationPrefix,
                    Description = "OneDrive folder - Requires OneDrive to be installed",
                    Result = OneDriveFolder
                },
                new StringPlaceholder
                {
                    Placeholder = _placeholderRetroArchScreenshots,
                    Type = _typePlaceholderFolderPath,
                    TypeLocalizationString = _localizationPrefix + _typePlaceholderFolderPathLocal,
                    LocalizationPrefix = _localizationPrefix,
                    Description = "RetroArch screenshots folder - Requires RetroArch to be installed",
                    Result = RetroArchScreenshotsDir
                },
                new StringPlaceholder
                {
                    Placeholder = _placeholderRomName,
                    Type = _typePlaceholderGameInfo,
                    TypeLocalizationString = _localizationPrefix + _typePlaceholderGameInfoLocal,
                    LocalizationPrefix = _localizationPrefix,
                    Description = "Name of the first ROM of the game without extension",
                    GameDependent = true,
                    ResultFunc = GetRomName
                },
                new StringPlaceholder
                {
                    Placeholder = _placeholderSteamAccountId,
                    Type = _typePlaceholderGameInfo,
                    TypeLocalizationString = _localizationPrefix + _typePlaceholderGameInfoLocal,
                    LocalizationPrefix = _localizationPrefix,
                    Description = "Steam account ID of the currently active Steam user",
                    Result = SteamAccountId
                },
                new StringPlaceholder
                {
                    Placeholder = _placeholderSteamDir,
                    Type = _typePlaceholderFolderPath,
                    TypeLocalizationString = _localizationPrefix + _typePlaceholderFolderPathLocal,
                    LocalizationPrefix = _localizationPrefix,
                    Description = "Steam installation folder - Requires Steam to be installed on the system",
                    Result = SteamInstallDir
                },
                new StringPlaceholder
                {
                    Placeholder = _placeholderSteamId,
                    Type = _typePlaceholderGameInfo,
                    TypeLocalizationString = _localizationPrefix + _typePlaceholderGameInfoLocal,
                    LocalizationPrefix = _localizationPrefix,
                    Description = "Game's Steam ID if it's a Steam game or has a steam link",
                    GameDependent = true,
                    ResultFunc = SteamHelper.GetSteamId
                },
                new StringPlaceholder
                {
                    Placeholder = _placeholderSteamScreenshotsDir,
                    Type = _typePlaceholderFolderPath,
                    TypeLocalizationString = _localizationPrefix + _typePlaceholderFolderPathLocal,
                    LocalizationPrefix = _localizationPrefix,
                    Description = "Steam screenshots folder - Requires Steam to be installed",
                    Result = SteamScreenshotsDir
                },
                new StringPlaceholder
                {
                    Placeholder = _placeholderUbisoftGameDir,
                    Type = _typePlaceholderFolderPath,
                    TypeLocalizationString = _localizationPrefix + _typePlaceholderFolderPathLocal,
                    LocalizationPrefix = _localizationPrefix,
                    Description = "Ubisoft games installation folder - Requires Ubisoft Connect to be installed",
                    Result = UbisoftGameDir
                },
                new StringPlaceholder
                {
                    Placeholder = _placeholderUbisoftInstallDir,
                    Type = _typePlaceholderFolderPath,
                    TypeLocalizationString = _localizationPrefix + _typePlaceholderFolderPathLocal,
                    LocalizationPrefix = _localizationPrefix,
                    Description = "Ubisoft Connect installation folder - Requires Ubisoft Connect to be installed",
                    Result = UbisoftInstallDir
                },
                new StringPlaceholder
                {
                    Placeholder = _placeholderUbisoftScreenshotsDir,
                    Type = _typePlaceholderFolderPath,
                    TypeLocalizationString = _localizationPrefix + _typePlaceholderFolderPathLocal,
                    LocalizationPrefix = _localizationPrefix,
                    Description = "Ubisoft Connect screenshots folder - Requires Ubisoft Connect to be installed",
                    Result = UbisoftScreenshotsDir
                },
                new StringPlaceholder
                {
                    Placeholder = _placeholderXboxGamebarScreenshotsDir,
                    Type = _typePlaceholderFolderPath,
                    TypeLocalizationString = _localizationPrefix + _typePlaceholderFolderPathLocal,
                    LocalizationPrefix = _localizationPrefix,
                    Description = "Xbox Game Bar screenshots folder",
                    Result = XboxGamebarScreenshotDir
                }
            };

            ////// Environment variables //////

            tmpPlaceholders.AddRange(Environment.GetEnvironmentVariables().Cast<DictionaryEntry>().Select(p => new StringPlaceholder
            {
                Placeholder = "{" + (string)p.Key + "}",
                Type = _typePlaceholderEnvVar,
                TypeLocalizationString = _localizationPrefix + _typePlaceholderEnvVarLocal,
                LocalizationPrefix = _localizationPrefix,
                Result = (string)p.Value
            }).OrderBy(p => p.Placeholder));

            _placeholders.AddMissing(tmpPlaceholders.OrderBy(p => p.Type.Equals(_typePlaceholderEnvVar) ? "X" : p.Type).ThenBy(p => p.Placeholder));
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
    }

    public class StringPlaceholder : ObservableObject
    {
        public string Description { get; set; }
        public string DescriptionLocalizationString => LocalizationPrefix + Placeholder.Replace("{", string.Empty).Replace("}", string.Empty) + "Description";
        public bool GameDependent { get; set; } = false;
        public bool IsPlayniteVar { get; set; } = false;
        public string LocalizationPrefix { get; set; }
        public string Placeholder { get; set; }
        public string Result { get; set; }
        public Func<Game, string> ResultFunc { get; set; }
        public string Type { get; set; }
        public string TypeLocalizationString { get; set; }
    }
}
