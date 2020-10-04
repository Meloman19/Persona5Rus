using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Persona5Rus.ViewModel
{
    class BindableBase : INotifyPropertyChanged
    {
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value))
                return false;

            storage = value;
            OnPropertChanged(propertyName);
            return true;
        }

        protected void OnPropertChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void RunInDispatcher(Action action)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher != null)
            {
                try
                {
                    dispatcher.Invoke(action);
                }
                catch { }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}