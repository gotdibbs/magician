using GalaSoft.MvvmLight.Messaging;
using Magician.Controls;
using Magician.ExtensionFramework;
using Magician.WebResourceLiveSync.Model;
using Magician.WebResourceLiveSync.ViewModels;
using System.Windows.Controls;

namespace Magician.WebResourceLiveSync.Views
{
    /// <summary>
    /// Interaction logic for Control.xaml
    /// </summary>
    [TrickDescription("Web Resource Live Sync", "Synchronize Web Resources on Save")]
    public partial class Control : Trick
    {
        public Control()
        {
            InitializeComponent();

            Loaded += Control_Loaded;
        }

        private void Control_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            var messenger = new Messenger();

            messenger.Register<UpdateHeaderMessage>(this, m => UpdateHeader(m));

            DataContext = new ControlViewModel(messenger);
        }

        private void UpdateHeader(UpdateHeaderMessage m)
        {
            if (Parent == null || !(Parent is TabItem))
            {
                return;
            }

            var tab = (TabItem)Parent;

            tab.Header = m.Header;
        }
    }
}
