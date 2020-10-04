using IniParser;
using IniParser.Model;

namespace Persona5Rus.Common
{
    internal sealed class Settings
    {
        private readonly FileIniDataParser iniParser = new FileIniDataParser();
        private string configPath;

        public void Init(string configPath)
        {
            this.configPath = configPath;
            try
            {
                var iniData = iniParser.ReadFile(configPath);

                CreateModCPK = iniData.Global.TryGetBool(nameof(CreateModCPK), true);
            }
            catch { }
        }

        public bool CreateModCPK { get; set; } = true;

        public void Save()
        {
            var iniData = new IniData();

            iniData.Global[nameof(CreateModCPK)] = CreateModCPK.ToString();

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
                configPath = configPath
            };
        }
    }

    internal static class IniExtensions
    {
        public static bool TryGetBool(this KeyDataCollection keyDatas, string key, bool @default)
        {
            var boolString = keyDatas[key];
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