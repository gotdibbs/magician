using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Magician.BulkWorkflowExecutor.Models;
using Magician.Connect;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;

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

        private Connector _connector;

        private OrganizationServiceProxy _service;

        private Messenger _messenger;

        public ControlViewModel(Messenger messenger)
        {
            _messenger = messenger;

            Connect();

            ConnectCommand = new RelayCommand(() => Connect());
            ExecuteCommand = new RelayCommand(() => Execute());
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
                var query = new QueryExpression("workflow");
                query.NoLock = true;
                query.Distinct = true;
                query.ColumnSet = new ColumnSet("name", "workflowid", "primaryentity");
                query.Criteria.AddCondition("ondemand", ConditionOperator.Equal, true);
                query.Criteria.AddCondition("primaryentity", ConditionOperator.NotNull);
                query.Criteria.AddCondition("statecode", ConditionOperator.Equal, 1); // active
                query.Criteria.AddCondition("statuscode", ConditionOperator.Equal, 2); // published
                query.Criteria.AddCondition("type", ConditionOperator.Equal, 1); // definition

                var result = _service.RetrieveMultiple(query);

                return result.Entities.Select(e =>
                {
                    return new Workflow
                    {
                        WorkflowId = e.Id,
                        Name = e["name"] as string,
                        LogicalName = e["primaryentity"] as string
                    };
                }).OrderBy(e => e.Name).ToList();
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
                var query = new QueryExpression("savedquery");
                query.NoLock = true;
                query.ColumnSet = new ColumnSet("name", "savedqueryid", "fetchxml");
                query.Criteria.AddCondition("querytype", ConditionOperator.Equal, 0);
                query.Criteria.AddCondition("returnedtypecode", ConditionOperator.Equal, entityLogicalName);
                query.Criteria.AddCondition("fetchxml", ConditionOperator.NotNull);
                query.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0); //active

                var results = _service.RetrieveMultiple(query);

                var views = results.Entities.Select(e =>
                {
                    return new View
                    {
                        ViewId = e.Id,
                        Name = e["name"] as string,
                        FetchXml = e["fetchxml"] as string
                    };
                }).ToList();

                query = new QueryExpression("userquery");
                query.NoLock = true;
                query.ColumnSet = new ColumnSet("name", "userqueryid", "fetchxml");
                query.Criteria.AddCondition("querytype", ConditionOperator.Equal, 0);
                query.Criteria.AddCondition("returnedtypecode", ConditionOperator.Equal, entityLogicalName);
                query.Criteria.AddCondition("fetchxml", ConditionOperator.NotNull);
                query.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0); //active

                results = _service.RetrieveMultiple(query);

                views.AddRange(results.Entities.Select(e =>
                {
                    return new View
                    {
                        ViewId = e.Id,
                        Name = e["name"] as string,
                        FetchXml = e["fetchxml"] as string
                    };
                }));

                views.Sort(delegate (View v1, View v2) { return v1.Name.CompareTo(v2.Name); });

                return views;
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
                var response = await ExecuteWorkflow(query, page, SelectedWorkflow.WorkflowId);

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
                var request = new FetchXmlToQueryExpressionRequest();
                request.FetchXml = view.FetchXml;
                var response = (FetchXmlToQueryExpressionResponse)_service.Execute(request);

                return response.Query;
            });
        }

        private Task<ExecuteResponse> ExecuteWorkflow(QueryExpression query, int page, Guid workflowId)
        {
            return Task.Run(() =>
            {
                var response = new ExecuteResponse();

                try
                {
                    query.PageInfo.Count = 100;
                    query.PageInfo.PageNumber = page;

                    var result = _service.RetrieveMultiple(query);

                    response.HasMoreResults = result.MoreRecords;
                    response.ProcessedCount = result.Entities.Count;

                    var em = new ExecuteMultipleRequest
                    {
                        Settings = new ExecuteMultipleSettings
                        {
                            ContinueOnError = false,
                            ReturnResponses = true
                        }
                    };

                    em.Requests = new OrganizationRequestCollection();
                    em.Requests.AddRange(result.Entities.Select(e => new ExecuteWorkflowRequest
                    {
                        EntityId = e.Id,
                        WorkflowId = workflowId
                    }));

                    var emResponse = (ExecuteMultipleResponse)_service.Execute(em);

                    if (emResponse.IsFaulted)
                    {
                        var firstFault = emResponse.Responses.Where(r => r.Fault != null)
                            .FirstOrDefault();

                        response.HasError = true;
                        response.ErrorMessage = firstFault != null ? firstFault.Fault.Message : "Unknown execute workflow error";

                        return response;
                    }
                }
                catch (Exception ex)
                {
                    response.HasError = true;
                    response.ErrorMessage = ex.Message;
                }

                return response;
            });
        }
    }
}
