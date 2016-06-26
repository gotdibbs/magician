using Magician.BulkWorkflowExecutor.Logic;
using Magician.BulkWorkflowExecutor.Logic.Models;
using Magician.DeployTeams.Logic;
using System;
using System.Management.Automation;

namespace Magician.Powershell
{
    [Cmdlet("Start", "ExecuteBulkWorkflow",
        DefaultParameterSetName = "Name based")]
    public class StartExecuteBulkWorkflow : CrmCmdlet
    {
        [Parameter(Mandatory = true, HelpMessage = "Logical name of the entity to run the workflow against")]
        [Alias("LogicalName")]
        public string EntityLogicalName { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "Name based", HelpMessage = "Name of workflow to execute")]
        public string WorkflowName { get; set; } = null;

        [Parameter(Mandatory = true, ParameterSetName = "Id based", HelpMessage = "Guid of workflow to execute")]
        public Guid WorkflowId { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "Name based", HelpMessage = "Name of view to run workflow against")]
        public string ViewName { get; set; } = null;

        [Parameter(Mandatory = true, ParameterSetName = "Id based", HelpMessage = "Guid of view to run workflow against")]
        public Guid ViewId { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "Specifies if the view is a user-specific view or a system user")]
        public SwitchParameter IsUserView { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                var service = ConnectToCrm();

                WriteDebug("Instantiating logic...");
                var logic = new ExecuteBulkWorkflowLogic(service);

                WriteDebug("Locating view...");
                View view = null;

                if (ViewId != Guid.Empty)
                {
                    WriteDebug("Locating view by Id...");
                    view = logic.RetrieveView(ViewId, EntityLogicalName, IsUserView.IsPresent);
                }
                else
                {
                    WriteDebug("Locating view by name...");
                    view = logic.RetrieveView(ViewName, EntityLogicalName, IsUserView.IsPresent);
                }

                if (view == null)
                {
                    throw new Exception("Could not locate the specified view.");
                }

                WriteDebug("Locating workflow...");
                Workflow workflow = null;

                if (WorkflowId != Guid.Empty)
                {
                    WriteDebug("Locating workflow by Id...");
                    workflow = logic.RetrieveWorkflow(WorkflowId);
                }
                else
                {
                    WriteDebug("Locating workflow by name...");
                    workflow = logic.RetrieveWorkflow(WorkflowName);
                }

                if (workflow == null)
                {
                    throw new Exception("Could not locate the specified workflow.");
                }

                WriteDebug("Transforming view to QueryExpression...");
                var query = logic.GetQuery(view.FetchXml);

                var moreRecords = true;
                var page = 1;
                var processedCount = 0;

                var progressRecord = new ProgressRecord(
                    0,
                    string.Format("Execute '{0}' against '{1}'.", workflow.Name, view.Name),
                    "Starting...");

                WriteProgress(progressRecord);

                do
                {
                    WriteDebug("Running workflow on page " + page);

                    var result = logic.Execute(query, workflow.WorkflowId, page);

                    if (result.HasError)
                    {
                        throw new Exception("Execute workflow failed. " + result.ErrorMessage);
                    }

                    processedCount += result.ProcessedCount;
                    moreRecords = result.HasMoreResults;

                    progressRecord.StatusDescription = string.Format("{0} records processed...", processedCount);
                    WriteProgress(progressRecord);

                    page++;
                } while (moreRecords);

                WriteDebug("Completed.");
            }
            catch (Exception ex)
            {
                ThrowTerminatingError(new ErrorRecord(ex, ex.GetType().Name, ErrorCategory.NotSpecified, null));
            }
        }
    }
}