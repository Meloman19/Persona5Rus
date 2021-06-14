using Microsoft.WindowsAPICodePack.Dialogs;
using Persona5Rus.Common;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
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

    internal sealed class SelectFileItem : SettingsItemBase
    {
        private string _path;
        private string _header;

        public SelectFileItem()
        {
            SelectFileCommand = new RelayCommand(SelectFile);
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

        public ICommand SelectFileCommand { get; }

        private void SelectFile(object obj)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = false;
            dialog.Filters.Add(new CommonFileDialogFilter("EBOOT", "*.BIN"));

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

        public event Action<SelectFileItem, string> PathChanged;
    }

    internal sealed class ComboBoxItem<T> : BindableBase
    {
        public T SourceT { get; set; }

        public string Value { get; set; }
    }

    internal class ComboBoxSelection<T> : SettingsItemBase
    {
        private ComboBoxItem<T> _selectedItem;

        public ComboBoxSelection()
        {
            Items = new ObservableCollection<ComboBoxItem<T>>();
        }

        public ComboBoxItem<T> SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (SetProperty(ref _selectedItem, value))
                {
                    if (_selectedItem != null)
                    {
                        ValueChanged?.Invoke(this, _selectedItem.SourceT);
                    }
                }
            }
        }

        public ObservableCollection<ComboBoxItem<T>> Items { get; }

        public event Action<ComboBoxSelection<T>, T> ValueChanged;
    }

    internal sealed class GameTypeSelect : ComboBoxSelection<Game>
    {

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
            var selectGame = new GameTypeSelect();
            selectGame.Items.Add(new ComboBoxItem<Game>()
            {
                SourceT = Game.Persona5PS3,
                Value = "Persona 5 - PS3"
            });
            selectGame.Items.Add(new ComboBoxItem<Game>()
            {
                SourceT = Game.Persona5PS4,
                Value = "Persona 5 - PS4"
            });
            selectGame.SelectedItem = selectGame.Items.FirstOrDefault(i => i.SourceT == Settings.GameType);
            selectGame.ValueChanged += (s, v) =>
            {
                Settings.GameType = v;
            };
            SettingsItems.Add(selectGame);

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
                Header = "Путь к распакованному ps3.cpk или ps4.cpk",
                Path = Settings.PsCPKPath
            };
            selectPsCPK.PathChanged += (s, v) =>
            {
                Settings.PsCPKPath = v;
            };
            SettingsItems.Add(selectPsCPK);

            var selectEBOOT = new SelectFileItem()
            {
                Header = "Путь к оригинальному файлу EBOOT.BIN",
                Path = Settings.EBOOTPath
            };
            selectEBOOT.PathChanged += (s, v) =>
            {
                Settings.EBOOTPath = v;
            };
            SettingsItems.Add(selectEBOOT);
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

        public bool DevSkipEBOOTImport
        {
            get => Settings.DevSkipEBOOTImport;
            set
            {
                if (Settings.DevSkipEBOOTImport != value)
                {
                    Settings.DevSkipEBOOTImport = value;
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