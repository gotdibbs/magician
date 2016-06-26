using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Management.Automation;

namespace Magician.Powershell
{
    public class CrmCmdlet : Cmdlet
    {
        [Parameter(Mandatory = true, HelpMessage = "Connection string for CRM (uses CrmConnector from Xrm Tooling)")]
        public string ConnectionString { get; set; }

        protected IOrganizationService ConnectToCrm()
        {
            //TraceControlSettings.TraceLevel = System.Diagnostics.SourceLevels.All;
            //TraceControlSettings.AddTraceListener(new TextWriterTraceListener("XrmTooling.txt"));

            WriteDebug("Instantiating connection...");
            var connector = new CrmServiceClient(ConnectionString);

            if (!connector.IsReady)
            {
                WriteDebug("Could not connect to CRM.");
                throw new Exception(connector.LastCrmError);
            }
            else
            {
                WriteDebug("Connected to " + connector.ConnectedOrgFriendlyName);
            }

            var service = connector.OrganizationServiceProxy;

            return service;
        }
    }
}
