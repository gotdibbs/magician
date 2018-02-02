using Magician.Connect.Views;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Net;

namespace Magician.Connect
{
    public class Connector
    {
        public CrmServiceClient CrmServiceClient { get; set; }

        public OrganizationServiceProxy OrganizationServiceProxy { get; set; }

        public string OrganizationFriendlyName { get; set; }

        public string OrganizationUniqueName { get; set; }

        public bool Connect()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            var connectionDialog = new ConnectWindow();
            connectionDialog.ShowDialog();

            if (connectionDialog.DialogResult == true)
            {
                CrmServiceClient = connectionDialog.Presenter.CrmConnectionMgr.CrmSvc;
                OrganizationFriendlyName = connectionDialog.Presenter.CrmConnectionMgr.ConnectedOrgFriendlyName;
                OrganizationUniqueName = connectionDialog.Presenter.CrmConnectionMgr.ConnectedOrgUniqueName;

                OrganizationServiceProxy = CrmServiceClient.OrganizationServiceProxy;

                return true;
            }

            return false;
        }
    }
}
