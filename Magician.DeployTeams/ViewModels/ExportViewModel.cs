using GalaSoft.MvvmLight.Command;
using Magician.Connect;
using Magician.DeployTeams.Logic;
using Magician.DeployTeams.Logic.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Magician.DeployTeams.ViewModels
{
    public class ExportViewModel : ImportExportViewModelBase
    {
        private ExportLogic _exportLogic;

        public ICommand ExportCommand { get; private set; }

        public ExportViewModel()
        {
            ConnectCommand = new RelayCommand(() => Connect());

            ExportCommand = new RelayCommand(() => Export());
        }

        private async void Connect()
        {
            if (_connector == null)
            {
                _connector = new Connector();
            }

            if (!_connector.Connect())
            {
                MessageBox.Show("Please click Connect to try connecting to Dynamics CRM again. A valid connection is required.");
                return;
            }

            IsBusy = true;
            IsConnected = false;

            if (_exportLogic == null)
            {
                _exportLogic = new ExportLogic(_connector.OrganizationServiceProxy);
            }
            else
            {
                _exportLogic.OrganizationService = _connector.OrganizationServiceProxy;
            }

            ConnectText = "Change from " + _connector.OrganizationFriendlyName;

            ProgressMessage = "Retrieving teams and security roles...";
            var teams = await RetrieveTeams();

            Teams = new ObservableCollection<Team>(teams);

            IsConnected = true;
            IsBusy = false;
        }


        private Task<List<Team>> RetrieveTeams()
        {
            return Task.Run(() =>
            {
                return _exportLogic.RetrieveTeams();
            });
        }

        private async void Export()
        {
            IsBusy = true;
            ProgressMessage = "Beginning export...";

            try
            {
                var dlg = new SaveFileDialog();
                dlg.FileName = string.Format("{0:yyyy-MM-dd} - Team Export from {1}",
                    DateTime.Now,
                    _connector.OrganizationFriendlyName);
                dlg.DefaultExt = ".json";
                dlg.Filter = "JSON Files |*.json";

                var result = dlg.ShowDialog();

                if (result != true)
                {
                    return;
                }

                ProgressMessage = "Exporting teams to file...";

                await Task.Run(() =>
                {
                    _exportLogic.Export(dlg.FileName, Teams);
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error was encountered while attempting to generate the export. Detail: " + ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
