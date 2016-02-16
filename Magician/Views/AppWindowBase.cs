using MahApps.Metro.Controls;
using System.Windows;
using System.Windows.Media;

namespace Magician.Views
{
    internal class AppWindowBase : MetroWindow
    {
        internal AppWindowBase()
        {
            //Background = FindResource("MaterialDesignPaper") as Brush;
            BorderBrush = new SolidColorBrush(Color.FromRgb(119, 119, 119));
            BorderThickness = new Thickness(1);
        }
    }
}
