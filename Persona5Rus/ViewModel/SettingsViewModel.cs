using Persona5Rus.Common;
using System;
using System.IO;

namespace Persona5Rus.ViewModel
{
    internal sealed class SettingsViewModel : BindableBase
    {
        private static readonly string BasePath = Path.GetDirectoryName(new Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath);
        private static readonly string ConfigPath = Path.Combine(BasePath, "Persona5Rus.ini");

        public SettingsViewModel()
        {
            Settings = new Settings();
            Settings.Init(ConfigPath);
        }

        public Settings Settings { get; }

        public bool CreateModCPK
        {
            get => Settings.CreateModCPK;
            set
            {
                if (Settings.CreateModCPK != value)
                {
                    Settings.CreateModCPK = value;
                    OnPropertChanged();
                }
            }
        }
    }
}