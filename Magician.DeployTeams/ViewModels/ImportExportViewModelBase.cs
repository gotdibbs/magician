using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using Magician.Connect;
using Magician.DeployTeams.Models;
using Microsoft.Xrm.Sdk.Client;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Magician.DeployTeams.ViewModels
{
    public class ImportExportViewModelBase : ViewModelBase
    {
        private ObservableCollection<Team> _teams;
        public ObservableCollection<Team> Teams
        {
            get { return _teams; }
            set { Set(ref _teams, value); }
        }

        private string _connectText = "Connect";
        public string ConnectText
        {
            get { return _connectText; }
            set { Set(ref _connectText, value); }
        }

        private string _progressMessage = "Loading...";
        public string ProgressMessage
        {
            get { return _progressMessage; }
            set { Set(ref _progressMessage, value); }
        }

        private bool _isBusy = false;
        public bool IsBusy
        {
            get { return _isBusy; }
            set
            {
                Set(ref _isBusy, value);

                if (value == false)
                {
                    ProgressMessage = "Loading...";
                }
            }
        }

        private bool _isConnected = false;
        public bool IsConnected
        {
            get { return _isConnected; }
            set { Set(ref _isConnected, value); }
        }

        public ICommand ConnectCommand { get; set; }

        protected Connector _connector;

        protected OrganizationServiceProxy _service;
    }
}
