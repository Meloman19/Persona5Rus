using Persona5Rus.ViewModel;
using System.ComponentModel;
using System.Windows;

namespace Persona5Rus
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
        }

        protected override void OnClosing(CancelEventArgs e)
        {            
            (DataContext as MainWindowViewModel)?.Release();
            base.OnClosing(e);
        }
    }
}