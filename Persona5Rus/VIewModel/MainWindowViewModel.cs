using System.Windows.Input;

namespace Persona5Rus.ViewModel
{
    internal sealed class MainWindowViewModel : BindableBase
    {
        private bool _onWork = false;

        public MainWindowViewModel()
        {
            MakeGoodCommand = new RelayCommand(MakeGood);

            SettingsVM = new SettingsViewModel();
            CreationVM = new CreationViewModel();
        }

        public SettingsViewModel SettingsVM { get; }

        public CreationViewModel CreationVM { get; }

        public ICommand MakeGoodCommand { get; }

        public bool OnWork
        {
            get { return _onWork; }
            set { SetProperty(ref _onWork, value); }
        }

        private void MakeGood(object obj)
        {
            if (OnWork)
            {
                return;
            }

            OnWork = true;
            SettingsVM.Settings.Save();
            CreationVM.MakeGood(SettingsVM.Settings);
        }

        public void Release()
        {
            if (OnWork)
            {
                return;
            }

            SettingsVM.Settings.Save();
        }
    }
}