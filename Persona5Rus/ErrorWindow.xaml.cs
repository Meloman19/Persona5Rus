using System.Windows;

namespace Persona5Rus
{
    public partial class ErrorWindow : Window
    {
        public ErrorWindow()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty ErrorTextProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(ErrorWindow));

        public string ErrorText
        {
            get { return (string)GetValue(ErrorTextProperty); }
            set { SetValue(ErrorTextProperty, value); }
        }
    }
}
