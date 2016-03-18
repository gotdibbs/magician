using System.Windows;
using System.Windows.Controls;

namespace Magician.ExtensionFramework.Controls
{
    /// <summary>
    /// Interaction logic for ErrorOverlay.xaml
    /// </summary>
    public partial class ErrorOverlay : UserControl
    {
        public string Message
        {
            get { return MessageText.Text; }
            set { MessageText.Text = value; }
        }

        public ErrorOverlay()
        {
            InitializeComponent();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Visibility = Visibility.Collapsed;
        }
    }
}
