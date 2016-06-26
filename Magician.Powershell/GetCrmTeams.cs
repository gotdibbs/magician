using Magician.DeployTeams.Logic;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation;

namespace Magician.Powershell
{
    [Cmdlet(VerbsCommon.Get, "CrmTeams",
            DefaultParameterSetName = "Name based export")]
    public class GetCrmTeams : CrmCmdlet
    {
        [Parameter(Mandatory = true, ParameterSetName = "Name based export", HelpMessage = "Names of teams to export from CRM")]
        public string[] TeamNames { get; set; } = null;

        [Parameter(Mandatory = true, ParameterSetName = "Id based export", HelpMessage = "Guids of teams to export from CRM")]
        public Guid[] TeamIds { get; set; } = null;

        private bool _exportAll = false;
        [Parameter(Mandatory = true, ParameterSetName = "Export everything", HelpMessage = "Indicates all exportable teams should be exported")]
        [Alias("All")]
        public SwitchParameter ExportAll
        {
            get { return _exportAll; }
            set { _exportAll = value; }
        }

        [Parameter(Mandatory = true, HelpMessage = "Destination filename for JSON output to be used by Set-CrmTeams")]
        public string FileName { get; set; }

        protected override void ProcessRecord()
        {
            //TraceControlSettings.TraceLevel = System.Diagnostics.SourceLevels.All;
            //TraceControlSettings.AddTraceListener(new TextWriterTraceListener("XrmTooling.txt"));

            try
            {
                var service = ConnectToCrm();

                WriteDebug("Instantiating logic...");
                var logic = new ExportLogic(service);

                WriteDebug("Retrieving teams...");
                var teams = logic.RetrieveTeams();

                if (ExportAll.IsPresent)
                {
                    WriteDebug("Exporting ALL teams...");
                    logic.Export(FileName, teams);
                }
                else if (TeamNames != null && TeamNames.Length > 0)
                {
                    WriteDebug("Exporting teams: " + string.Join(", ", TeamNames));
                    logic.Export(FileName, teams.Where(t => TeamNames.Contains(t.Name)));
                }
                else if (TeamIds != null && TeamIds.Length > 0)
                {
                    WriteDebug("Exporting teams with Ids: " + string.Join(", ", TeamIds));
                    logic.Export(FileName, teams.Where(t => TeamIds.Contains(t.TeamId)));
                }
                else
                {
                    throw new ArgumentException("Encountered unexpected combination of arguments. Please review documentation and try again.");
                }
            }
            catch (Exception ex)
            {
                ThrowTerminatingError(new ErrorRecord(ex, ex.GetType().Name, ErrorCategory.NotSpecified, null));
            }
        }
    }
}