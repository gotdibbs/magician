using Magician.Controls;
using Magician.DeployTeams.ViewModels;
using Magician.ExtensionFramework;

namespace Magician.DeployTeams.Views
{
    /// <summary>
    /// Interaction logic for Control.xaml
    /// </summary>
    [TrickDescription("Deploy Teams", "Select team records from a source organization to sync with another organization.")]
    public partial class Control : Trick
    {
        public Control()
        {
            InitializeComponent();

            DataContext = new ControlViewModel();
        }
    }
}
