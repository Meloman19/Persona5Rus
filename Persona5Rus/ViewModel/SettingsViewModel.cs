using Microsoft.WindowsAPICodePack.Dialogs;
using Persona5Rus.Common;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;

namespace Persona5Rus.ViewModel
{
    internal abstract class SettingsItemBase : BindableBase
    {

    }

    internal sealed class SelectFolderItem : SettingsItemBase
    {
        private string _path;
        private string _header;

        public SelectFolderItem()
        {
            SelectFolderCommand = new RelayCommand(SelectFolder);
        }

        public string Header
        {
            get => _header;
            set => SetProperty(ref _header, value);
        }

        public string Path
        {
            get => _path;
            set
            {
                if (SetProperty(ref _path, value))
                {
                    PathChanged?.Invoke(this, _path);
                }
            }
        }

        public ICommand SelectFolderCommand { get; }

        private void SelectFolder(object obj)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;

            var initialDir = "";
            try
            {
                initialDir = System.IO.Path.GetFullPath(Path);
            }
            catch { }

            dialog.InitialDirectory = initialDir;

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                Path = dialog.FileName;
            }

            dialog.Dispose();
        }

        public event Action<SelectFolderItem, string> PathChanged;
    }

    internal sealed class SettingsViewModel : BindableBase
    {
        private static readonly string BasePath = Path.GetDirectoryName(new Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath);
        private static readonly string ConfigPath = Path.Combine(BasePath, "Persona5Rus.ini");

        public SettingsViewModel()
        {
            Settings = new Settings();
            Settings.Init(ConfigPath);

            Fill();
        }

        private void Fill()
        {
            var selectDataCPK = new SelectFolderItem()
            {
                Header = "Путь к распакованному data.cpk",
                Path = Settings.DataCPKPath
            };
            selectDataCPK.PathChanged += (s, v) =>
            {
                Settings.DataCPKPath = v;
            };
            SettingsItems.Add(selectDataCPK);

            var selectPsCPK = new SelectFolderItem()
            {
                Header = "Путь к распакованному ps3.cpk",
                Path = Settings.PsCPKPath
            };
            selectPsCPK.PathChanged += (s, v) =>
            {
                Settings.PsCPKPath = v;
            };
            SettingsItems.Add(selectPsCPK);
        }

        public Settings Settings { get; }

        public ObservableCollection<SettingsItemBase> SettingsItems { get; } = new ObservableCollection<SettingsItemBase>();

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

        public bool DevSkipTextImport
        {
            get => Settings.DevSkipTextImport;
            set
            {
                if (Settings.DevSkipTextImport != value)
                {
                    Settings.DevSkipTextImport = value;
                    OnPropertChanged();
                }
            }
        }

        public bool DevSkipTextureImport
        {
            get => Settings.DevSkipTextureImport;
            set
            {
                if (Settings.DevSkipTextureImport != value)
                {
                    Settings.DevSkipTextureImport = value;
                    OnPropertChanged();
                }
            }
        }

        public bool DevSkipMovieImport
        {
            get => Settings.DevSkipMovieImport;
            set
            {
                if (Settings.DevSkipMovieImport != value)
                {
                    Settings.DevSkipMovieImport = value;
                    OnPropertChanged();
                }
            }
        }
    }
}