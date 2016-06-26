using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Magician.BulkWorkflowExecutor.Logic;
using Magician.BulkWorkflowExecutor.Logic.Models;
using Magician.BulkWorkflowExecutor.Models;
using Magician.Connect;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Magician.BulkWorkflowExecutor.ViewModels
{
    public class ControlViewModel : ViewModelBase
    {
        private ObservableCollection<Workflow> _workflows;
        public ObservableCollection<Workflow> Workflows
        {
            get { return _workflows; }
            set { Set(ref _workflows, value); }
        }

        private Workflow _selectedWorkflow;
        public Workflow SelectedWorkflow
        {
            get { return _selectedWorkflow; }
            set
            {
                Set(ref _selectedWorkflow, value);

                LoadViews();
                RaisePropertyChanged(() => IsReadyForExecute);
            }
        }

        private ObservableCollection<View> _views;
        public ObservableCollection<View> Views
        {
            get { return _views; }
            set { Set(ref _views, value); }
        }

        private View _selectedView;
        public View SelectedView
        {
            get { return _selectedView; }
            set
            {
                Set(ref _selectedView, value);

                RaisePropertyChanged(() => IsReadyForExecute);
            }
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

        public bool IsReadyForExecute
        {
            get
            {
                return SelectedView != null && SelectedWorkflow != null;
            }
        }

        private bool _isBusy = true;
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

        public ICommand ConnectCommand { get; set; }

        public ICommand ExecuteCommand { get; set; }

        public ICommand OnUnloadedCommand { get; set; }

        private bool _isUnloaded = false;

        private Connector _connector;

        private ExecuteBulkWorkflowLogic _logic;

        private Messenger _messenger;

        public ControlViewModel(Messenger messenger)
        {
            _messenger = messenger;

            Connect();

            ConnectCommand = new RelayCommand(() => Connect());
            ExecuteCommand = new RelayCommand(() => Execute());
            OnUnloadedCommand = new RelayCommand(() => OnUnloaded());
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

            if (_logic == null)
            {
                _logic = new ExecuteBulkWorkflowLogic(_connector.OrganizationServiceProxy);
            }
            else
            {
                _logic.OrganizationService = _connector.OrganizationServiceProxy;
            }

            ConnectText = "Reconnect";

            _messenger.Send(new UpdateHeaderMessage
            {
                Header = "Bulk Workflow Executor: " + _connector.OrganizationFriendlyName
            });

            var workflows = await RetrieveWorkflows();

            Workflows = new ObservableCollection<Workflow>(workflows);

            IsBusy = false;
        }

        private Task<List<Workflow>> RetrieveWorkflows()
        {
            return Task.Run(() =>
            {
                return _logic.RetrieveWorkflows();
            });
        }

        private async void LoadViews()
        {
            if (SelectedWorkflow == null)
            {
                return;
            }

            IsBusy = true;

            var views = await RetrieveViews(SelectedWorkflow.LogicalName);

            Views = new ObservableCollection<View>(views);

            IsBusy = false;
        }

        private Task<List<View>> RetrieveViews(string entityLogicalName)
        {
            return Task.Run(() =>
            {
                return _logic.RetrieveViews(entityLogicalName);
            });
        }

        private async void Execute()
        {
            if (MessageBox.Show("If you continue, this workflow will run against ALL records in the view. This could seriously affect performance for all users in your environment and could potentially run on thousands of records. Shall we continue?",
                string.Empty, MessageBoxButton.YesNo) == MessageBoxResult.No)
            {
                return;
            }

            IsBusy = true;

            var query = await GetQuery(SelectedView);
            var moreResults = true;
            var processedCount = 0;
            var page = 1;

            do
            {
                // Cancel if the tab has been closed
                if (_isUnloaded == true)
                {
                    moreResults = false;
                    continue;
                }

                var response = await ExecuteWorkflow(query, SelectedWorkflow.WorkflowId, page);

                if (response.HasError)
                {
                    MessageBox.Show(string.Format("An error was encountered. {0} records were processed.\n\n{1}",
                        processedCount,
                        response.ErrorMessage));

                    moreResults = false;
                }
                else
                {
                    moreResults = response.HasMoreResults;

                    processedCount += response.ProcessedCount;

                    page++;

                    ProgressMessage = string.Format("Processed {0} records...", processedCount);
                }
            } while (moreResults);

            IsBusy = false;
        }

        private Task<QueryExpression> GetQuery(View view)
        {
            return Task.Run(() =>
            {
                return _logic.GetQuery(view.FetchXml);
            });
        }

        private Task<ExecuteResponse> ExecuteWorkflow(QueryExpression query, Guid workflowId, int page)
        {
            return Task.Run(() =>
            {
                return _logic.Execute(query, workflowId, page);
            });
        }

        private void OnUnloaded()
        {
            _isUnloaded = true;
        }
    }
}
