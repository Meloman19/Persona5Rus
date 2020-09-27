using Persona5Rus.ViewModel;
using System.Windows;

namespace Persona5Rus
{
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            //Rework.DoNormal();

            MainWindow = new MainWindow()
            {
                DataContext = new MainWindowViewModel()
            };
            MainWindow.Show();
        }
    }
}