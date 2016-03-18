using GalaSoft.MvvmLight.Messaging;
using Magician.BulkWorkflowExecutor.Models;
using Magician.BulkWorkflowExecutor.ViewModels;
using Magician.ExtensionFramework;
using Magician.ExtensionFramework.Controls;
using System.Windows.Controls;

namespace Magician.BulkWorkflowExecutor.Views
{
    /// <summary>
    /// Interaction logic for Control.xaml
    /// </summary>
    [TrickDescription("Bulk Workflow Executor", "Select a view of records, then pick an on-demand workflow on run against all records in that view.")]
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
