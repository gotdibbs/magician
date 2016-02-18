using GalaSoft.MvvmLight.Messaging;
using Magician.Attributes;
using Magician.Controls;
using Magician.UsersByRole.Models;
using Magician.UsersByRole.ViewModels;
using System.Windows.Controls;

namespace Magician.UsersByRole.Views
{
    /// <summary>
    /// Interaction logic for Control.xaml
    /// </summary>
    [Trick("Users by Role", "Select a security role, see all the users that have it.")]
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
