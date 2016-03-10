using GalaSoft.MvvmLight;

namespace Magician.DeployTeams.ViewModels
{
    public class ControlViewModel : ViewModelBase
    {
        public ExportViewModel ExportViewModel { get; } = new ExportViewModel();

        public ImportViewModel ImportViewModel { get; } = new ImportViewModel();
    }
}
