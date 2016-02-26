using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Magician.Connect;
using Magician.WebResourceLiveSync.Model;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Xrm.Sdk;
using System.IO;

namespace Magician.WebResourceLiveSync.ViewModels
{
    public class ControlViewModel : ViewModelBase
    {
        private ObservableCollection<Solution> _solutions;
        public ObservableCollection<Solution> Solutions
        {
            get { return _solutions; }
            set { Set(ref _solutions, value); }
        }

        private Solution _selectedSolution;
        public Solution SelectedSolution
        {
            get { return _selectedSolution; }
            set
            {
                Set(ref _selectedSolution, value);

                RefreshTree();
            }
        }

        private string _localDirectory;
        public string LocalDirectory
        {
            get { return _localDirectory; }
            set
            {
                Set(ref _localDirectory, value);

                RefreshTree();
            }
        }

        private List<DirectoryItem> _files;
        public List<DirectoryItem> Files
        {
            get { return _files; }
            set { Set(ref _files, value); }
        }

        private string _connectText = "Connect";
        public string ConnectText
        {
            get { return _connectText; }
            set { Set(ref _connectText, value); }
        }

        private bool _isBusy = true;
        public bool IsBusy
        {
            get { return _isBusy; }
            set { Set(ref _isBusy, value); }
        }

        public ICommand ConnectCommand { get; set; }

        private Connector _connector;

        private OrganizationServiceProxy _service;

        private Messenger _messenger;

        private object _treeLock = new object();

        // TODO: store profiles solution/directory combo (future)
        // TODO: Reload tree view when solution or directory change
        // TODO: filewatcher
        // TODO: crm connection
        // TODO: pull solutions
        // TODO: compare modifiedon/entity stamp/new has changes request
        // TODO: scan directory for matches ahead of time
        public ControlViewModel(Messenger messenger)
        {
            _messenger = messenger;
            IsBusy = false;
            //Connect();

            ConnectCommand = new RelayCommand(() => Connect());
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

            _service = _connector.OrganizationServiceProxy;

            ConnectText = "Reconnect";

            _messenger.Send(new UpdateHeaderMessage
            {
                Header = "Users by Role: " + _connector.OrganizationFriendlyName
            });

            var solutions = await LoadSolutions();

            Solutions = new ObservableCollection<Solution>(solutions);

            IsBusy = false;
        }

        private Task<IOrderedEnumerable<Solution>> LoadSolutions()
        {
            return Task.Run(() =>
            {
                var query = new QueryExpression("solution");
                query.NoLock = true;
                query.ColumnSet = new ColumnSet("uniquename", "solutionid", "publisherid");
                query.Criteria.AddCondition("ismanaged", ConditionOperator.Equal, false);
                query.Criteria.AddCondition("uniquename", ConditionOperator.NotIn, new string[] { "Active", "Basic" });
                query.AddOrder("uniquename", OrderType.Ascending);
                var publisher = query.AddLink("publisher", "publisherid", "publisherid", JoinOperator.Inner);
                publisher.EntityAlias = "publisher";
                publisher.Columns = new ColumnSet("customizationprefix");

                var result = _service.RetrieveMultiple(query);

                var solutions = result.Entities.Select(e => new Solution
                {
                    SolutionId = e.Id,
                    Name = e["uniquename"] as string,
                    PublisherId = GetPublisherId(e),
                    CustomizationPrefix = GetCustomizationPrefix(e)
                });

                return solutions.OrderBy(r => r.Name);
            });
        }

        private Guid GetPublisherId(Entity e)
        {
            if (e.Contains("publisherid"))
            {
                var er = e.GetAttributeValue<EntityReference>("publisherid");

                if (er != null)
                {
                    return er.Id;
                }
            }

            throw new Exception(string.Format("Could not locate the publisher for solution {0}.", e["uniquename"]));
        }

        private string GetCustomizationPrefix(Entity e)
        {
            var prefix = string.Empty;

            if (e.Contains("publisher.customizationprefix"))
            {
                var alias = e.GetAttributeValue<AliasedValue>("publisher.customizationprefix");

                prefix = alias != null ? alias.Value as string : string.Empty;
            }

            if (string.IsNullOrEmpty(prefix))
            {
                throw new Exception(string.Format("Could not parse customization prefix for solution {0}.", e["uniquename"]));
            }

            return prefix;
        }

        private async void RefreshTree()
        {
            lock (_treeLock)
            {
                var directory = ScanDirectory();

                Files = new List<DirectoryItem>(new DirectoryItem[] { directory });
            }
        }

        private DirectoryItem ScanDirectory()
        {
            var rootDirectoryInfo = new DirectoryInfo(LocalDirectory);

            return GetContents(rootDirectoryInfo);
        }

        private DirectoryItem GetContents(DirectoryInfo directoryInfo)
        {
            var directoryNode = new DirectoryItem { Name = directoryInfo.Name };
            foreach (var directory in directoryInfo.GetDirectories())
            {
                directoryNode.Items.Add(GetContents(directory));
            }

            foreach (var file in directoryInfo.GetFiles())
            {
                directoryNode.Items.Add(new DirectoryItem { Name = file.Name });
            }

            return directoryNode;
        }
    }
}
