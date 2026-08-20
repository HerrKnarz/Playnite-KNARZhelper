using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace KNARZhelper
{
    /// <summary>
    /// Class to match external platform names to the existing platforms in Playnite. Shamelessly
    /// partly copied from Jeshibu: https://github.com/Jeshibu/PlayniteExtensions/blob/590a4a10d2223b12ecc742d908707ab34841ea65/source/PlayniteExtensions.Common/PlatformUtility.cs
    /// </summary>
    public class PlatformHelper
    {
        private readonly Dictionary<string, string[]> _platformSpecNameByNormalName;
#pragma warning disable IDE0090 // Use 'new(...)'
        private readonly Regex _trimCompanyName = new Regex(@"^(atari|bandai|coleco|commodore|mattel|nec|nintendo|sega|sinclair|snk|sony|microsoft)?\s+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private readonly Regex _trimInput = new Regex(@"^(pal|jpn?|usa?|ntsc)\s+|[™®©]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
#pragma warning restore IDE0090 // Use 'new(...)'

        /// <summary>
        /// Initializes a new instance of the <see cref="PlatformHelper"/> class.
        /// </summary>
        /// <param name="platforms">List of platforms to initialize the helper with</param>
        public PlatformHelper(IEnumerable<Platform> platforms)
        {
            _platformSpecNameByNormalName = new Dictionary<string, string[]>(StringComparer.InvariantCultureIgnoreCase);

            RefreshPlatformList(platforms);
        }

        /// <summary>
        /// returns all platforms created in Playnite that fit the platform name
        /// </summary>
        /// <param name="platformName">Name of the platform</param>
        /// <returns>List of platforms</returns>
        public IEnumerable<MetadataProperty> GetPlatforms(string platformName) => GetPlatforms(platformName, strict: false);

        /// <summary>
        /// returns all platforms created in Playnite that fit the platform name
        /// </summary>
        /// <param name="platformName">Name of the platform</param>
        /// <param name="strict">
        /// If true, only matches will be returned. If false the function also returns all
        /// platforms, that aren't found at all.
        /// </param>
        /// <returns>List of platforms</returns>
        public IEnumerable<MetadataProperty> GetPlatforms(string platformName, bool strict)
        {
            if (string.IsNullOrWhiteSpace(platformName))
            {
                return new List<MetadataProperty>();
            }

            var sanitizedPlatformName = _trimInput.Replace(platformName, string.Empty);

            return _platformSpecNameByNormalName.TryGetValue(sanitizedPlatformName, out var specIds)
                ? specIds.Select(s => new MetadataSpecProperty(s)).ToList<MetadataProperty>()
                : strict
                    ? new List<MetadataProperty>()
                    : new List<MetadataProperty> { new MetadataNameProperty(sanitizedPlatformName) };
        }

        public void RefreshPlatformList(IEnumerable<Platform> platforms)
        {
            _platformSpecNameByNormalName.Clear();

            foreach (var platform in platforms.Where(p => p.SpecificationId != null))
            {
                TryAddPlatformByName(_platformSpecNameByNormalName, platform.Name, platform.SpecificationId);

                var nameWithoutCompany = _trimCompanyName.Replace(platform.Name, string.Empty);

                if (!_platformSpecNameByNormalName.ContainsKey(nameWithoutCompany))
                {
                    _platformSpecNameByNormalName.Add(nameWithoutCompany, new[] { platform.SpecificationId });
                }
            }

            TryAddPlatformByName(_platformSpecNameByNormalName, "3DO", "3do");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Flash", "Adobe Flash", "FLA" }, "adobe_flash");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Amstrad CPC", "CPC" }, "amstrad_cpc");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Apple II", "AP2" }, "apple_2");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Arcade", "ARCD" }, "arcade");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Arduboy", "ARDB" }, "arduboy");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Atari 2600", "2600" }, "atari_2600");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Atari 5200", "5200" }, "atari_5200");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Atari 7800", "7800" }, "atari_7800");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Atari 8-bit", "A800", "Atari 800", "Atari 400", "Atari 400/800" }, "atari_8bit");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Atari Falcon030", "Falcon030", "FALC" }, "atari_falcon030");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Atari Jaguar", "Jaguar", "JAG" }, "atari_jaguar");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Atari Lynx", "Lynx" }, "atari_lynx");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Atari ST/STE", "Atari ST", "ST" }, "atari_st");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Bandai WonderSwan Color", "WonderSwan Color" }, "bandai_wonderswan_color");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Bandai WonderSwan", "WonderSwan" }, "bandai_wonderswan");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Coleco ColecoVision", "ColecoVision", "Col" }, "coleco_vision");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Commodore 64", "C64" }, "commodore_64");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Commodore Amiga CD32", "Amiga CD32", "CD32" }, "commodore_amiga_cd32");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Commodore Amiga", "Amiga", "AGA", "OCS", "Amiga OCS", "Amiga AGA" }, "commodore_amiga");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Commodore CBM-5x0", "CBM-5x0" }, "commodore_cbm5x0");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Commodore CBM-II", "CBM-II" }, "commodore_cbm2");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Commodore PET", "PET" }, "commodore_pet");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Commodore Plus/4", "Plus/4" }, "commodore_plus4");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Commodore VIC20", "VIC20", "VIC-20" }, "commodore_vci20");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Fairchild Channel F", "Channel F", "FAIR" }, "fairchild_channelf");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Mac", "OSX", "OS X", "MacOS", "Mac OS", "Mac OS X", "Macintosh" }, "macintosh");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Magnavox Odyssey 2", "Odyssey 2", "ODY2" }, "magnavox_odyssey_2");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Mattel Intellivision", "Intellivision", "INTV" }, "mattel_intellivision");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Mega Duck" }, "megaduck");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Microsoft MSX", "MSX" }, "microsoft_msx");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Microsoft MSX2", "MSX2" }, "microsoft_msx2");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "NEC PC-88", "PC-88", "8801", "NEC PC8801" }, "nec_pc88");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "NEC PC-98", "PC-98", "9801", "NEC PC9801" }, "nec_pc98");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "NEC PC-FX", "PC-FX", "PCFX" }, "nec_pcfx");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "NEC SuperGrafx", "SuperGrafx", "PC Engine SuperGrafx", "SGFX" }, "nec_supergrafx");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "NEC TurboGrafx 16", "TurboGrafx 16", "PC Engine", "NEC PC Engine", "TurboGrafx", "PCE" }, "nec_turbografx_16");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "NEC TurboGrafx-CD", "TurboGrafx-CD", "PC Engine CD", "CD-ROM²", "PECD" }, "nec_turbografx_cd");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Nintendo 3DS", "3DS" }, "nintendo_3ds");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "N64", "Nintendo 64" }, "nintendo_64");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Nintendo DS", "DS", "NDS" }, "nintendo_ds");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Nintendo DSi", "DSi" }, "nintendo_dsi");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Nintendo Family Computer Disk System", "Famicom Disk", "FDSY" }, "nintendo_famicom_disk");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Nintendo Game & Watch", "Game & Watch", "WTCH" }, "nintendo_gameandwatch");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "GB", "Game Boy", "Nintendo Game Boy" }, "nintendo_gameboy");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "GBA", "Game Boy Advance", "Nintendo Game Boy Advance" }, "nintendo_gameboyadvance");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "GBC", "Game Boy Color", "Nintendo Game Boy Color" }, "nintendo_gameboycolor");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Nintendo GameCube", "GameCube", "GC" }, "nintendo_gamecube");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "NES", "Nintendo Entertainment System" }, "nintendo_nes");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "SNES", "Super NES", "Super Nintendo Entertainment System" }, "nintendo_super_nes");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Nintendo Switch", "Switch", "SWTC" }, "nintendo_switch");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Nintendo Switch 2", "Switch 2", "SWT2" }, "nintendo_switch2");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Nintendo Virtual Boy", "Virtual Boy", "VB" }, "nintendo_virtualboy");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Nintendo Wii", "Wii" }, "nintendo_wii");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Nintendo Wii U", "Wii U", "WiiU" }, "nintendo_wiiu");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "DOS", "MS-DOS" }, "pc_dos");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Linux", "Lin" }, "pc_linux");
            TryAddPlatformByName(_platformSpecNameByNormalName,
                new[] { "Microsoft Windows", "Windows", "PC", "PC CD-ROM", "PC DVD", "PC DVD-ROM", "Windows 95", "WIN", "W31" }, "pc_windows");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Philips CD-i", "CD-i", "CDI" }, "philips_cdi");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Pokémon mini", "Pokemon mini", "POKM" }, "pokemon_mini");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Sega 32X", "32X", "Mega Drive 32X", "Sega Mega Drive 32X" }, "sega_32x");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Sega CD", "Mega CD", "Sega Mega CD", "Sega-CD", "Mega-CD", "SCD" }, "sega_cd");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Sega Dreamcast", "Dreamcast", "DC" }, "sega_dreamcast");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Sega Game Gear", "Game Gear", "GG" }, "sega_gamegear");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "gen", "Sega Genesis", "Genesis", "Sega Mega Drive", "Mega Drive", "Mega Drive/Genesis" }, "sega_genesis");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "SMS", "Master System", "Sega Master System" }, "sega_mastersystem");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "SAT", "Saturn", "Sega Saturn" }, "sega_saturn");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Sega SG-1000", "SG-1000", "SG1K" }, "sega_sg1000");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Sharp X1", "SX1" }, "sharp_x1");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Sharp X68000", "X68000", "X68K" }, "sharp_x68000");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Sinclair ZX Spectrum +3", "ZX Spectrum +3" }, "sinclair_zxspectrum3");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Sinclair ZX Spectrum", "ZX Spectrum", "SPC" }, "sinclair_zxspectrum");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Sinclair ZX81", "ZX81", "ZX 81" }, "sinclair_zx81");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "SNK Neo Geo MVS", "Neo Geo MVS", "SNK Neo Geo AES", "Neo Geo AES", "NEO", "Neo-Geo" }, "snk_neogeo_aes");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "SNK Neo Geo CD", "Neo Geo CD" }, "snk_neogeo_cd");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "SNK Neo Geo Pocket", "Neo Geo Pocket", "NGP" }, "snk_neogeopocket");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "SNK Neo Geo Pocket Color", "Neo Geo Pocket Color", "NGPC" }, "snk_neogeopocket_color");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "PS", "PS1", "PSX", "Playstation", "Sony Playstation" }, "sony_playstation");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "PS2", "Playstation 2", "Sony Playstation 2" }, "sony_playstation2");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "PS3", "Playstation 3", "Sony Playstation 3" }, "sony_playstation3");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "PS4", "Playstation 4", "Sony Playstation 4" }, "sony_playstation4");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "PS4/5", "Playstation 4/5" }, "sony_playstation4", "sony_playstation5");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "PS5", "Playstation 5", "Sony Playstation 5" }, "sony_playstation5");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "PSP", "Playstation Portable", "Sony Playstation Portable" }, "sony_psp");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Vita", "Playstation Vita", "Sony Playstation Vita" }, "sony_vita");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Texas Instruments TI-83", "TI-83", "TICT" }, "ti_83");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Thomson MO5", "MO5", "THMS" }, "thomson_mo5");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Thomson TO7", "TO7", "THMS" }, "thomson_to7");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "TIC-80" }, "tic_80");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Uzebox", "UZBX" }, "uzebox");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "GCE Vectrex", "Vectrex", "VEC" }, "vectrex");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "WASM-4" }, "wasm4");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Watara Supervision", "Supervision", "SV" }, "watara_supervision");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Microsoft Xbox", "Xbox" }, "xbox");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Microsoft Xbox 360", "Xbox 360", "X360" }, "xbox360");
            TryAddPlatformByName(_platformSpecNameByNormalName, new[] { "Microsoft Xbox One", "Xbox One", "XBOX1" }, "xbox_one");
            TryAddPlatformByName(_platformSpecNameByNormalName,
                new[]
                {
                    "Microsoft Xbox Series X", "Microsoft Xbox Series S", "Xbox Series X", "Xbox Series S",
                    "Microsoft Xbox Series X/S", "Microsoft Xbox Series S/X", "Xbox Series X/S", "Xbox Series S/X",
                    "Xbox Series X|S", "XBSX"
                }, "xbox_series");
        }

        /// <summary>
        /// Tries to add a platform to the dictionary by its name.
        /// </summary>
        /// <param name="dict">Dictionary to add the platform to</param>
        /// <param name="platformName">Name of the platform</param>
        /// <param name="platformSpecNames">Specification names of the platform</param>
        /// <returns>True if the platform was added, false if it already exists</returns>
        //TODO: Replace the new dictionary method TryAdd in P11!
        private static bool TryAddPlatformByName(IDictionary<string, string[]> dict, string platformName, params string[] platformSpecNames)
        {
            if (dict.ContainsKey(platformName))
            {
                return false;
            }

            dict.Add(platformName, platformSpecNames);
            return true;
        }

        /// <summary>
        /// Tries to add multiple platforms to the dictionary by their names.
        /// </summary>
        /// <param name="dict">Dictionary to add the platforms to</param>
        /// <param name="platformNames">Names of the platforms</param>
        /// <param name="platformSpecNames">Specification names of the platforms</param>
        /// <returns>True if all platforms were added, false if any already exist</returns>
        private static bool TryAddPlatformByName(IDictionary<string, string[]> dict, IEnumerable<string> platformNames, params string[] platformSpecNames)
            => platformNames.Aggregate(true, (current, platformName) => current & TryAddPlatformByName(dict, platformName, platformSpecNames));
    }
}
