using IniParser;
using IniParser.Model;
using System;

namespace Persona5Rus.Common
{
    internal enum Game
    {
        Persona5PS3,
        Persona5PS4
    }

    internal sealed class Settings
    {
        private const string DevKey = "Dev";

        private readonly FileIniDataParser iniParser = new FileIniDataParser();
        private string configPath;

        public void Init(string configPath)
        {
            this.configPath = configPath;
            try
            {
                var iniData = iniParser.ReadFile(configPath);

                CreateModCPK = iniData.TryGetBool(null, nameof(CreateModCPK), true);

                if (Enum.TryParse<Game>(iniData.Global[nameof(GameType)], out Game result))
                {
                    GameType = result;
                }
                DataCPKPath = iniData.Global[nameof(DataCPKPath)] ?? string.Empty;
                PsCPKPath = iniData.Global[nameof(PsCPKPath)] ?? string.Empty;
                EBOOTPath = iniData.Global[nameof(EBOOTPath)] ?? string.Empty;

                DevSkipTextImport = iniData.TryGetBool(DevKey, nameof(DevSkipTextImport), false);
                DevSkipTextureImport = iniData.TryGetBool(DevKey, nameof(DevSkipTextureImport), false);
                DevSkipEBOOTImport = iniData.TryGetBool(DevKey, nameof(DevSkipEBOOTImport), false);
                DevSkipMovieImport = iniData.TryGetBool(DevKey, nameof(DevSkipMovieImport), false);
            }
            catch { }
        }

        public Game GameType { get; set; } = Game.Persona5PS3;

        public string DataCPKPath { get; set; } = string.Empty;

        public string PsCPKPath { get; set; } = string.Empty;

        public string EBOOTPath { get; set; } = string.Empty;

        public bool CreateModCPK { get; set; } = true;

        public bool DevSkipTextImport { get; set; } = false;

        public bool DevSkipTextureImport { get; set; } = false;

        public bool DevSkipEBOOTImport { get; set; } = false;

        public bool DevSkipMovieImport { get; set; } = false;

        public void Save()
        {
            var iniData = new IniData();

            iniData.Global[nameof(CreateModCPK)] = CreateModCPK.ToString();
            iniData.Global[nameof(GameType)] = GameType.ToString();
            iniData.Global[nameof(DataCPKPath)] = DataCPKPath ?? string.Empty;
            iniData.Global[nameof(PsCPKPath)] = PsCPKPath ?? string.Empty;
            iniData.Global[nameof(EBOOTPath)] = EBOOTPath ?? string.Empty;

            iniData.Sections.AddSection(DevKey);
            iniData.Sections[DevKey][nameof(DevSkipTextImport)] = DevSkipTextImport.ToString();
            iniData.Sections[DevKey][nameof(DevSkipTextureImport)] = DevSkipTextureImport.ToString();
            iniData.Sections[DevKey][nameof(DevSkipEBOOTImport)] = DevSkipEBOOTImport.ToString();
            iniData.Sections[DevKey][nameof(DevSkipMovieImport)] = DevSkipMovieImport.ToString();

            try
            {
                iniParser.WriteFile(configPath, iniData);                
            }
            catch { }
        }

        public Settings Copy()
        {
            return new Settings()
            {
                GameType = GameType,
                DataCPKPath = DataCPKPath,
                PsCPKPath = PsCPKPath,
                CreateModCPK = CreateModCPK,
                EBOOTPath = EBOOTPath,
                DevSkipTextImport = DevSkipTextImport,
                DevSkipTextureImport = DevSkipTextureImport,
                DevSkipEBOOTImport = DevSkipEBOOTImport,
                DevSkipMovieImport = DevSkipMovieImport,
                configPath = configPath
            };
        }
    }

    internal static class IniExtensions
    {
        public static bool TryGetBool(this IniData data, string groupKey, string key, bool @default)
        {
            KeyDataCollection group;
            if (groupKey == null)
            {
                group = data.Global;
            }
            else
            {
                group = data.Sections[groupKey];
            }

            if (group == null)
            {
                return @default;
            }

            var boolString = group[key];
            if (boolString == null)
            {
                return @default;
            }

            if (!bool.TryParse(boolString, out bool @return))
            {
                return @default;
            }

            return @return;
        }
    }
}