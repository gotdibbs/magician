using Magician.ExtensionFramework;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Magician.Controls
{
    public class Trick : UserControl, ITrick
    {
        public Trick()
        {
            Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new System.Uri("/Magician;component/Resources/ResourceDictionary.xaml", UriKind.Relative)
            });
        }
    }
}
