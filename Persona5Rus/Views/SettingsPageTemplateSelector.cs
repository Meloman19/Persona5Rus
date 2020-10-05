using Persona5Rus.ViewModel;
using System.Windows;
using System.Windows.Controls;

namespace Persona5Rus.Views
{
    internal sealed class SettingsPageTemplateSelector : DataTemplateSelector
    {
        public DataTemplate SelectFolderTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is SelectFolderItem)
            {
                return SelectFolderTemplate;
            }

            return null;
        }
    }
}