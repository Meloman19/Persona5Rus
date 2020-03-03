using Persona5Rus.VIewModel;
using System.Windows;

namespace Persona5Rus
{
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            MainWindow = new MainWindow()
            {
                DataContext = new MainWindowViewModel()
            };
            MainWindow.Show();
        }
    }
}