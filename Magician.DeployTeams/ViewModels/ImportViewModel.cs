using GalaSoft.MvvmLight.Command;
using Magician.Connect;
using Magician.DeployTeams.Logic;
using Magician.DeployTeams.Logic.Models;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Magician.DeployTeams.ViewModels
{
    public class ImportViewModel : ImportExportViewModelBase
    {
        private ImportLogic _importLogic;

        public ICommand LoadCommand { get; private set; }
        public ICommand DeployCommand { get; private set; }

        public ImportViewModel()
        {
            ConnectCommand = new RelayCommand(() => Connect());

            LoadCommand = new RelayCommand(() => LoadExportData());
            DeployCommand = new RelayCommand(() => Deploy());
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

            if (_importLogic == null)
            {
                _importLogic = new ImportLogic(_connector.OrganizationServiceProxy);
            }
            else
            {
                _importLogic.OrganizationService = _connector.OrganizationServiceProxy;
            }

            ConnectText = "Change from " + _connector.OrganizationFriendlyName;

            await LoadExportData();

            IsConnected = true;
            IsBusy = false;
        }

        private async Task LoadExportData()
        {
            IsBusy = true;
            ProgressMessage = "Beginning data load to memory...";

            try
            {
                var dlg = new OpenFileDialog();
                dlg.DefaultExt = ".json";
                dlg.Filter = "JSON Files |*.json";

                var result = dlg.ShowDialog();

                if (result == true)
                {
                    ProgressMessage = "Loading exported data...";
                    var filename = dlg.FileName;

                    var teams = await Task.Run(() =>
                    {
                        return _importLogic.DeserializeTeamsFromFile(filename);
                    });

                    Teams = new ObservableCollection<Team>(teams);
                }
                else
                {
                    MessageBox.Show("You must select a file to deploy.");
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error was encountered while attempting to load the export. Detail: " + ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async void Deploy()
        {
            try
            {
                var selected = Teams.Where(t => t.IsSelected == true).ToList();

                if (selected.Count < 1)
                {
                    MessageBox.Show("You must select at least one team to deploy.");
                    return;
                }

                IsBusy = true;

                ProgressMessage = "Upserting teams...";
                await Task.Run(() => _importLogic.Import(selected));

                IsBusy = false;
            }
            catch
            {
                throw;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
