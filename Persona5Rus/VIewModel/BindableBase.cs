using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Persona5Rus.VIewModel
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
            Application.Current.Dispatcher.Invoke(action);
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}