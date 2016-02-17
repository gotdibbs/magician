using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Controls;
using Microsoft.Xrm.Sdk;
using Microsoft.Crm.Sdk.Messages;
using System.Windows;
using Microsoft.Xrm.Sdk.Query;
using Magician.Connect;

namespace Magician.TestRecipe.ViewModels
{
    public class TestRecipeViewModel : ViewModelBase
    {
        private bool _isConnected;
        public bool IsConnected
        {
            get { return _isConnected; }
            set { Set(ref _isConnected, value); }
        }

        private string _userId;
        public string UserId
        {
            get { return _userId; }
            set { Set(ref _userId, value); }
        }

        private string _userName;
        public string UserName
        {
            get { return _userName; }
            set { Set(ref _userName, value); }
        }

        private string _orgName;
        public string OrgName
        {
            get { return _orgName; }
            set { Set(ref _orgName, value); }
        }

        public Connector Connector { get; set; }

        public IOrganizationService Service { get; set; }

        public ICommand ConnectCommand { get; private set; }

        public TestRecipeViewModel()
        {
            ConnectCommand = new RelayCommand(() => Connect());

            IsConnected = false;
        }

        public void Connect()
        {
            if (Connector == null)
            {
                Connector = new Connector();
            }

            if (!Connector.Connect())
            {
                return;
            }

            IsConnected = true;

            Service = Connector.OrganizationServiceProxy;

            OrgName = Connector.OrganizationFriendlyName;

            if (Service == null)
            {
                return;
            }

            var i = (WhoAmIResponse)Service.Execute(new WhoAmIRequest());

            UserId = i.UserId.ToString();

            var user = Service.Retrieve("systemuser", i.UserId, new ColumnSet("fullname"));

            UserName = user.Contains("fullname") ? user["fullname"] as string : null;
        }
    }
}
