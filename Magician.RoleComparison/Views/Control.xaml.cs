using GalaSoft.MvvmLight.Messaging;
using Magician.Controls;
using Magician.ExtensionFramework;
using Magician.RoleCompare.Models;
using Magician.RoleCompare.ViewModels;
using System.Windows.Controls;

namespace Magician.RoleCompare.Views
{
    /// <summary>
    /// Interaction logic for Control.xaml
    /// </summary>
    [TrickDescription("Role Compare", "Compare the privileges assigned to two different roles.")]
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
