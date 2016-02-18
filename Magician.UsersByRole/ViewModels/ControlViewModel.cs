using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Magician.Connect;
using Magician.UsersByRole.Models;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Query;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Magician.UsersByRole.ViewModels
{
    public class ControlViewModel : ViewModelBase
    {
        private ObservableCollection<Role> _roles;
        public ObservableCollection<Role> Roles
        {
            get { return _roles; }
            set { Set(ref _roles, value); }
        }

        private Role _selectedRole;
        public Role SelectedRole
        {
            get { return _selectedRole; }
            set
            {
                Set(ref _selectedRole, value);

                LoadUsers();
            }
        }

        private ObservableCollection<User> _users;
        public ObservableCollection<User> Users
        {
            get { return _users; }
            set { Set(ref _users, value); }
        }

        private string _connectText = "Connect";
        public string ConnectText
        {
            get { return _connectText; }
            set { Set(ref _connectText, value); }
        }

        private bool _isComparing = false;
        public bool IsComparing
        {
            get { return _isComparing; }
            set { Set(ref _isComparing, value); }
        }

        public ICommand ConnectCommand { get; set; }

        private Connector _connector;

        private OrganizationServiceProxy _service;

        private Messenger _messenger;

        public ControlViewModel(Messenger messenger)
        {
            _messenger = messenger;

            Connect();

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

            _service = _connector.OrganizationServiceProxy;

            ConnectText = "Reconnect";

            _messenger.Send(new UpdateHeaderMessage
            {
                Header = "Users by Role: " + _connector.OrganizationFriendlyName
            });

            var roles = await LoadRoles();

            Roles = new ObservableCollection<Role>(roles);
        }

        private Task<IEnumerable<Role>> LoadRoles()
        {
            return Task.Run(() =>
            {
                var query = new QueryExpression("role");
                query.NoLock = true;
                query.ColumnSet = new ColumnSet(true);
                query.AddOrder("name", OrderType.Ascending);
                var bu = query.AddLink("businessunit", "businessunitid", "businessunitid");
                bu.LinkCriteria.AddCondition("parentbusinessunitid", ConditionOperator.Null);

                var result = _service.RetrieveMultiple(query);

                var roles = result.Entities.Select(e => new Role
                {
                    RoleId = e.Id,
                    Name = e["name"] as string
                });

                return roles;
            });
        }

        private async void LoadUsers()
        {
            if (SelectedRole == null)
            {
                return;
            }

            var users = await RetrieveUsers();

            Users = new ObservableCollection<User>(users);
        }

        private Task<IEnumerable<User>> RetrieveUsers()
        {
            return Task.Run(() =>
            {
                var query = new QueryExpression("systemuser");
                query.NoLock = true;
                query.ColumnSet = new ColumnSet("domainname", "fullname", "systemuserid");
                query.AddOrder("fullname", OrderType.Ascending);
                var userroles = query.AddLink("systemuserroles", "systemuserid", "systemuserid");
                userroles.LinkCriteria.AddCondition("roleid", ConditionOperator.Equal, SelectedRole.RoleId);

                var result = _service.RetrieveMultiple(query);

                var users = result.Entities.Select(e => new User
                {
                    UserId = e.Id,
                    DomainName = e.Contains("domainname") ? e["domainname"] as string : null,
                    FullName = e.Contains("fullname") ? e["fullname"] as string : null
                });

                return users;
            });
        }
    }
}
