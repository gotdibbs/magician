using System;
using System.Windows;
using System.Windows.Controls;

namespace Magician.Controls
{
    public class RecipeBase : UserControl
    {
        public RecipeBase()
        {
            Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new System.Uri("/Magician;component/Resources/ResourceDictionary.xaml", UriKind.Relative)
            });
        }
    }
}
