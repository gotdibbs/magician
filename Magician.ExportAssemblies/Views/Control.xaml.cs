using GalaSoft.MvvmLight.Messaging;
using Magician.Attributes;
using Magician.Controls;
using Magician.ExportAssemblies.Models;
using Magician.ExportAssemblies.ViewModels;
using System.Windows.Controls;

namespace Magician.ExportAssemblies.Views
{
    /// <summary>
    /// Interaction logic for Control.xaml
    /// </summary>
    [Trick("Export Assemblies", "Selectively export any plugin or workflow assemblies registered in a Dynamics CRM Organization.")]
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
