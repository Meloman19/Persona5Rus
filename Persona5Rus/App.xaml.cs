using AuxiliaryLibraries.Tools;
using Persona5Rus.ViewModel;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Documents;

namespace Persona5Rus
{
    public partial class App : Application
    {
        private MainWindowViewModel MainWindowViewModel;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            //var cpk = @"d:\Visual Studio 2019\Project\Persona5Rus\Persona5Rus\bin\Debug\Source\cpk";
            //var files = new List<string>();
            //foreach(var file in Directory.EnumerateFiles(cpk, "*", SearchOption.AllDirectories))
            //{
            //    files.Add(IOTools.RelativePath(file, cpk));
            //}
            //File.WriteAllLines(@"d:\Visual Studio 2019\Project\Persona5Rus\Persona5Rus\bin\Debug\Source\work_files.txt", files);

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