using Persona5Rus.ViewModel;
using System.Windows;

namespace Persona5Rus
{
    public partial class App : Application
    {
        private MainWindowViewModel MainWindowViewModel;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            //Rework.DoNormal();

            MainWindow = new MainWindow()
            {
                DataContext = MainWindowViewModel = new MainWindowViewModel()
            };
            MainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            MainWindowViewModel?.Release();
        }
    }
}