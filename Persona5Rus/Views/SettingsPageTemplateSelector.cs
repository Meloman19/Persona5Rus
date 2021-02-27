using Persona5Rus.ViewModel;
using System.Windows;
using System.Windows.Controls;

namespace Persona5Rus.Views
{
    internal sealed class SettingsPageTemplateSelector : DataTemplateSelector
    {
        public DataTemplate SelectFolderTemplate { get; set; }

        public DataTemplate SelectFileTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            switch (item)
            {
                case SelectFolderItem folderItem:
                    return SelectFolderTemplate;
                case SelectFileItem fileItem:
                    return SelectFileTemplate;
            }

            return null;
        }
    }
}