using CsvHelper;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Magician.Connect;
using Magician.RoleCompare.Models;
using Microsoft.Win32;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Magician.RoleCompare.ViewModels
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

                SetSecondaryRoles();
                Compare();
            }
        }

        private ObservableCollection<Role> _secondaryRoles;
        public ObservableCollection<Role> SecondaryRoles
        {
            get { return _secondaryRoles; }
            set { Set(ref _secondaryRoles, value); }
        }

        private Role _selectedSecondaryRole;
        public Role SelectedSecondaryRole
        {
            get { return _selectedSecondaryRole; }
            set
            {
                Set(ref _selectedSecondaryRole, value);

                Compare();
            }
        }

        private ObservableCollection<Comparison> _comparisons;
        public ObservableCollection<Comparison> Comparisons
        {
            get { return _comparisons; }
            set { Set(ref _comparisons, value); }
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

        private bool _isBusy = true;
        public bool IsBusy
        {
            get { return _isBusy; }
            set { Set(ref _isBusy, value); }
        }

        public ICommand ConnectCommand { get; set; }

        public ICommand ExportCommand { get; set; }

        private Connector _connector;

        private OrganizationServiceProxy _service;

        private Messenger _messenger;

        public ControlViewModel(Messenger messenger)
        {
            _messenger = messenger;

            Connect();

            ConnectCommand = new RelayCommand(() => Connect());
            ExportCommand = new RelayCommand(() => Export());
        }

        private async void Connect()
        {
            if (_connector == null)
            {
                _connector = new Connector();
            }

            if (!_connector.Connect())
            {
                MessageBox.Show("Please click Connect to try connecting to Dynamics CRM again. Role comparison requires a valid connection.");
                return;
            }

            IsBusy = true;

            _service = _connector.OrganizationServiceProxy;

            ConnectText = "Reconnect";

            _messenger.Send(new UpdateHeaderMessage
            {
                Header = "Role Compare: " + _connector.OrganizationFriendlyName
            });

            var roles = await LoadRoles();

            Roles = new ObservableCollection<Role>(roles);

            IsBusy = false;
        }

        private Task<IEnumerable<Role>> LoadRoles()
        {
            return Task.Run(() =>
            {
                var query = new QueryExpression("role");
                query.NoLock = true;
                query.ColumnSet = new ColumnSet("name", "roleid");
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

        private void SetSecondaryRoles()
        {
            if (SelectedRole == null)
            {
                SecondaryRoles.Clear();
                return;
            }

            SecondaryRoles = new ObservableCollection<Role>(Roles.Where(r => r.Name != SelectedRole.Name));
        }

        private async void Compare()
        {
            if (SelectedRole == null || SelectedSecondaryRole == null)
            {
                return;
            }

            IsBusy = true;

            var comparisons = new List<Comparison>();

            var privileges1 = await LoadPrivileges(SelectedRole.RoleId);

            foreach (var p in privileges1)
            {
                var comparison = new Comparison
                {
                    PrivilegeId = p.PrivilegeId,
                    Name = p.Name,
                    Depth1 = p.Depth,
                    Depth2 = string.Empty
                };

                comparisons.Add(comparison);
            }

            var privileges2 = await LoadPrivileges(SelectedSecondaryRole.RoleId);

            foreach (var p in privileges2)
            {
                var comparison = comparisons.Where(c => c.PrivilegeId == p.PrivilegeId).FirstOrDefault();

                if (comparison == null)
                {
                    comparison = new Comparison
                    {
                        PrivilegeId = p.PrivilegeId,
                        Name = p.Name,
                        Depth1 = string.Empty,
                        Depth2 = p.Depth
                    };

                    comparisons.Add(comparison);
                }
                else
                {
                    comparison.Depth2 = p.Depth;
                }
            }

            comparisons.Sort(delegate(Comparison c1, Comparison c2) { return c1.Name.CompareTo(c2.Name); });

            Comparisons = new ObservableCollection<Comparison>(comparisons);

            IsComparing = true;

            IsBusy = false;
        }

        private Task<List<Privilege>> LoadPrivileges(Guid roleId)
        {
            return Task.Run(() =>
            {
                var query = new QueryExpression("privilege");
                query.NoLock = true;
                query.ColumnSet = new ColumnSet("name");
                query.AddOrder("name", OrderType.Ascending);
                var intersect = query.AddLink("roleprivileges", "privilegeid", "privilegeid");
                intersect.EntityAlias = "roleprivilege";
                intersect.Columns = new ColumnSet("privilegedepthmask");
                var role = intersect.AddLink("role", "roleid", "roleid");
                role.LinkCriteria.AddCondition("roleid", ConditionOperator.Equal, roleId);

                var result = _service.RetrieveMultiple(query);

                return result.Entities.Select(e => new Privilege
                {
                    PrivilegeId = e.Id,
                    Name = e["name"] as string,
                    Depth = GetDepth(e)
                }).ToList();
            });
        }

        private string GetDepth(Entity e)
        {
            try
            {
                var alias = e.GetAttributeValue<AliasedValue>("roleprivilege.privilegedepthmask");
                var depth = (int)alias.Value;

                switch ((PrivilegeDepthMask)depth)
                {
                    case PrivilegeDepthMask.Basic:
                        return "User";
                    case PrivilegeDepthMask.Local:
                        return "Business Unit";
                    case PrivilegeDepthMask.Deep:
                        return "Parent: Child";
                    case PrivilegeDepthMask.Global:
                        return "Organization";
                }

                return "UKNOWN";
            }
            catch
            {
                MessageBox.Show("Error encountered in GetDepth");
                return "ERROR";
            }
        }

        private async void Export()
        {
            IsBusy = true;

            var temp = Path.GetTempPath();
            var tempFile = Path.Combine(temp, "comparison.csv");

            await WriteComparison(tempFile);

            var dlg = new SaveFileDialog();
            dlg.FileName = string.Format("Comparison of {0} vs {1}",
                SelectedRole.Name,
                SelectedSecondaryRole.Name);
            dlg.DefaultExt = ".csv";
            dlg.Filter = "CSV Files (.csv)|.csv";

            var result = dlg.ShowDialog();

            if (result == true)
            {
                var filename = dlg.FileName;
                File.Move(tempFile, filename);
            }
            else
            {
                File.Delete(tempFile);
            }

            IsBusy = false;
        }

        private Task WriteComparison(string tempFile)
        {
            return Task.Run(() =>
            {
                using (var sw = new StreamWriter(tempFile))
                {
                    var writer = new CsvWriter(sw);
                    writer.WriteField("Privilege ID");
                    writer.WriteField("Privilege Name");
                    writer.WriteField("Privileges Match");
                    writer.WriteField(SelectedRole.Name + " Privilege Depth");
                    writer.WriteField(SelectedSecondaryRole.Name + " Privilege Depth");

                    writer.NextRecord();

                    foreach (var record in Comparisons)
                    {
                        writer.WriteField(record.PrivilegeId);
                        writer.WriteField(record.Name);
                        writer.WriteField(record.IsMatchYesNo);
                        writer.WriteField(record.Depth1);
                        writer.WriteField(record.Depth2);

                        writer.NextRecord();
                    }
                }
            });
        }
    }
}