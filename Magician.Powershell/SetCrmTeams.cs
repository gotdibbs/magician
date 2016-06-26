using Magician.DeployTeams.Logic;
using System;
using System.Management.Automation;

namespace Magician.Powershell
{
    [Cmdlet(VerbsCommon.Set, "CrmTeams")]
    public class SetCrmTeams : CrmCmdlet
    {
        [Parameter(Mandatory = true, HelpMessage = "Connection string for CRM (uses CrmConnector from Xrm Tooling)")]
        public string ConnectionString { get; set; }

        [Parameter(Mandatory = true, HelpMessage = "Path to JSON file created using Get-CrmTeams command")]
        public string FileName { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                var service = ConnectToCrm();

                WriteDebug("Instantiating logic...");
                var logic = new ImportLogic(service);

                WriteDebug("Loading teams...");
                var teams = logic.DeserializeTeamsFromFile(FileName);

                WriteDebug("Upserting teams...");
                logic.Import(teams);
            }
            catch (Exception ex)
            {
                ThrowTerminatingError(new ErrorRecord(ex, ex.GetType().Name, ErrorCategory.NotSpecified, null));
            }
        }
    }
}