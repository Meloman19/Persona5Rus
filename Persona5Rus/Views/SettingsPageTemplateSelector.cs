using Persona5Rus.ViewModel;
using System.Windows;
using System.Windows.Controls;

namespace Persona5Rus.Views
{
    internal sealed class SettingsPageTemplateSelector : DataTemplateSelector
    {
        public DataTemplate SelectFolderTemplate { get; set; }

        public DataTemplate SelectFileTemplate { get; set; }

        public DataTemplate GameSelectionTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            switch (item)
            {
                case SelectFolderItem _:
                    return SelectFolderTemplate;
                case SelectFileItem _:
                    return SelectFileTemplate;
                case GameTypeSelect _:
                    return GameSelectionTemplate;
            }

            return null;
        }
    }
}