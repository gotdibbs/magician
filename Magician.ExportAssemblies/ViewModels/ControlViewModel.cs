using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Magician.Connect;
using Magician.ExportAssemblies.Models;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using MessageBox = System.Windows.MessageBox;

namespace Magician.ExportAssemblies.ViewModels
{
    public class ControlViewModel : ViewModelBase
    {
        private ObservableCollection<Assembly> _assemblies;
        public ObservableCollection<Assembly> Assemblies
        {
            get { return _assemblies; }
            set { Set(ref _assemblies, value); }
        }

        private string _connectText = "Connect";
        public string ConnectText
        {
            get { return _connectText; }
            set { Set(ref _connectText, value); }
        }

        private bool _isConnected = false;
        public bool IsConnected
        {
            get { return _isConnected; }
            set { Set(ref _isConnected, value); }
        }

        private bool _isBusy = true;
        public bool IsBusy
        {
            get { return _isBusy; }
            set { Set(ref _isBusy, value); }
        }

        public ICommand ConnectCommand { get; set; }

        public ICommand ExportCommand { get; set; }

        private Connector _connector;

        private OrganizationServiceProxy _service;

        private Messenger _messenger;

        public ControlViewModel(Messenger messenger)
        {
            _messenger = messenger;

            Connect();

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

            IsConnected = true;

            _service = _connector.OrganizationServiceProxy;

            ConnectText = "Reconnect";

            _messenger.Send(new UpdateHeaderMessage
            {
                Header = "Export Assemblies: " + _connector.OrganizationFriendlyName
            });

            var assemblies = await LoadAssemblies();

            Assemblies = new ObservableCollection<Assembly>(assemblies);

            IsBusy = false;
        }

        private Task<List<Assembly>> LoadAssemblies()
        {
            return Task.Run(() =>
            {
                var query = new QueryExpression("pluginassembly");
                query.Distinct = true;
                query.NoLock = true;
                query.ColumnSet = new ColumnSet("name", "pluginassemblyid");
                query.AddOrder("name", OrderType.Ascending);
                var type = query.AddLink("plugintype", "pluginassemblyid", "pluginassemblyid", JoinOperator.Inner);
                type.LinkCriteria.AddCondition("typename", ConditionOperator.NotLike, "Microsoft.Crm.%");
                type.LinkCriteria.AddCondition("typename", ConditionOperator.NotLike, "ActivityFeeds.%");

                var result = _service.RetrieveMultiple(query);

                var assemblies = result.Entities.Select(e => new Assembly
                {
                    AssemblyId = e.Id,
                    Export = false,
                    Name = e["name"] as string,
                    RegisteredStepCount = 0
                }).ToList();

                foreach (var assembly in assemblies)
                {
                    query = new QueryExpression("sdkmessageprocessingstep");
                    query.Distinct = true;
                    query.NoLock = true;
                    query.ColumnSet = new ColumnSet("sdkmessageprocessingstepid");
                    type = query.AddLink("plugintype", "plugintypeid", "plugintypeid");
                    type.LinkCriteria.AddCondition("pluginassemblyid", ConditionOperator.Equal, assembly.AssemblyId);

                    result = _service.RetrieveMultiple(query);

                    assembly.RegisteredStepCount = result.Entities.Count;
                }

                return assemblies;
            });
        }

        private async void Export()
        {
            if (Assemblies.Count == 0)
            {
                MessageBox.Show("There are no assemblies to export.");
                return;
            }

            if (!Assemblies.Any(a => a.Export == true))
            {
                MessageBox.Show("Please select at least one assembly to export.");
                return;
            }

            var folderBrowser = new FolderBrowserDialog
            {
                Description = "Please select a location to save the selected assemblies",
                ShowNewFolderButton = true
            };

            var dialogResult = folderBrowser.ShowDialog();

            if (dialogResult == DialogResult.OK &&
                !string.IsNullOrEmpty(folderBrowser.SelectedPath))
            {
                IsBusy = true;

                foreach (var assembly in Assemblies.Where(a => a.Export == true))
                {
                    var path = Path.Combine(folderBrowser.SelectedPath, assembly.Name + ".dll");

                    var bytes = await DownloadAssembly(assembly.AssemblyId);

                    File.WriteAllBytes(path, bytes);
                }

                IsBusy = false;
            }

            var toExport = Assemblies.Where(a => a.Export == true).ToList();
        }

        private Task<byte[]> DownloadAssembly(Guid assemblyId)
        {
            return Task.Run(() =>
            {
                var assembly = _service.Retrieve("pluginassembly", assemblyId, new ColumnSet("content"));

                return Convert.FromBase64String(assembly["content"] as string);
            });
        }
    }
}
