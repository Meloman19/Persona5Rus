﻿using IniParser;
using IniParser.Model;

namespace Persona5Rus.Common
{
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

                DevSkipTextImport = iniData.TryGetBool(DevKey, nameof(DevSkipTextImport), false);
                DevSkipTableImport = iniData.TryGetBool(DevKey, nameof(DevSkipTableImport), false);
                DevSkipMovieImport = iniData.TryGetBool(DevKey, nameof(DevSkipMovieImport), false);
            }
            catch { }
        }

        public bool CreateModCPK { get; set; } = true;

        public bool DevSkipTextImport { get; set; } = false;

        public bool DevSkipTableImport { get; set; } = false;

        public bool DevSkipMovieImport { get; set; } = false;

        public void Save()
        {
            var iniData = new IniData();

            iniData.Global[nameof(CreateModCPK)] = CreateModCPK.ToString();

            iniData.Sections.AddSection(DevKey);
            iniData.Sections[DevKey][nameof(DevSkipTextImport)] = DevSkipTextImport.ToString();
            iniData.Sections[DevKey][nameof(DevSkipTableImport)] = DevSkipTableImport.ToString();
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
                CreateModCPK = CreateModCPK,
                DevSkipTextImport = DevSkipTextImport,
                DevSkipTableImport = DevSkipTableImport,
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