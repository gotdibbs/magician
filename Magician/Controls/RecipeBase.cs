using Magician.Connect.Views;
using Microsoft.Xrm.Sdk;
using System.Windows.Controls;

namespace Magician.Controls
{
    public class RecipeBase : TabItem
    {
        public IOrganizationService Connect()
        {
            var connectionDialog = new ConnectWindow();
            connectionDialog.ShowDialog();

            if (connectionDialog.DialogResult == true)
            {
                return connectionDialog.Presenter.CrmConnectionMgr.CrmSvc.OrganizationServiceProxy;
            }

            return null;
        }
    }
}
