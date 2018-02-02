using CsvHelper;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Magician.Connect;
using Magician.RoleCompare.Models;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Win32;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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

        private bool _isConnected = false;
        public bool IsConnected
        {
            get { return _isConnected; }
            set { Set(ref _isConnected, value); }
        }

        private bool _isBusy = true;
        public bool IsBusy
        {
            get { return _isBusy; }
            set { Set(ref _isBusy, value); }
        }

        public ICommand ConnectCommand { get; set; }

        public ICommand ExportCommand { get; set; }

        public ICommand ExportAllCommand { get; set; }

        private List<EntityMetadata> _entityMetadata;

        private Connector _connector;

        private OrganizationServiceProxy _service;

        private Messenger _messenger;

        public ControlViewModel(Messenger messenger)
        {
            _messenger = messenger;

            Connect();

            ConnectCommand = new RelayCommand(() => Connect());
            ExportCommand = new RelayCommand(() => Export());
            ExportAllCommand = new RelayCommand(() => ExportAll());
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

            IsConnected = true;

            _service = _connector.OrganizationServiceProxy;

            ConnectText = "Reconnect";

            _messenger.Send(new UpdateHeaderMessage
            {
                Header = "Role Compare: " + _connector.OrganizationFriendlyName
            });

            _entityMetadata = await LoadEntityMetadata();

            var roles = await LoadRoles();

            Roles = new ObservableCollection<Role>(roles);

            IsBusy = false;
        }

        private Task<List<EntityMetadata>> LoadEntityMetadata()
        {
            return Task.Run(() =>
            {
                var req = new RetrieveAllEntitiesRequest
                {
                    EntityFilters = EntityFilters.Entity,
                    RetrieveAsIfPublished = false
                };

                var result = (RetrieveAllEntitiesResponse)_service.Execute(req);

                return result.EntityMetadata.ToList();
            });
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

        private async Task<List<RoleExport>> GetAll()
        {
            var results = new List<RoleExport>();

            foreach (var role in Roles)
            {
                var privileges = await LoadPrivileges(role.RoleId);

                foreach (var p in privileges)
                {
                    var export = new RoleExport
                    {
                        PrivilegeId = p.PrivilegeId,
                        EntityName = p.LogicalName,
                        AccessRight = p.AccessRight,
                        PrivilegeName = p.Name,
                        RoleName = role.Name,
                        Depth = p.Depth,
                    };

                    results.Add(export);
                }
            }

            results.Sort(delegate (RoleExport r1, RoleExport r2) {
                // Sort By Role Name Ascending
                var result = r1.RoleName.CompareTo(r2.RoleName);
                // Then Sort By Entity Display Name
                result = result != 0 ? result : r1.EntityName.CompareTo(r2.EntityName);
                // Finally Sorty By Access Right (Create, Read, etc.)
                return result != 0 ? result : r1.AccessRight.CompareTo(r2.AccessRight);
            });

            return results;
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
                    EntityName = p.LogicalName,
                    AccessRight = p.AccessRight,
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
                        EntityName = p.LogicalName,
                        AccessRight = p.AccessRight,
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

            comparisons.Sort(delegate(Comparison c1, Comparison c2) {
                // Sort By Match Ascending (No first)
                var result = c1.IsMatchYesNo.CompareTo(c2.IsMatchYesNo);
                // Then Sort By Entity Display Name
                result = result != 0 ? result : c1.EntityName.CompareTo(c2.EntityName);
                // Finally Sorty By Access Right (Create, Read, etc.)
                return result != 0 ? result : c1.AccessRight.CompareTo(c2.AccessRight);
            });

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
                query.ColumnSet = new ColumnSet("name", "accessright");
                query.AddOrder("name", OrderType.Ascending);

                var typecode = query.AddLink("privilegeobjecttypecodes", "privilegeid", "privilegeid", JoinOperator.LeftOuter);
                typecode.EntityAlias = "privilegeobjecttypecode";
                typecode.Columns = new ColumnSet("objecttypecode");

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
                    AccessRight = GetAccessRight(e),
                    LogicalName = GetEntityName(e),
                    Depth = GetDepth(e)
                }).ToList();
            });
        }

        private string GetAccessRight(Entity e)
        {
            try
            {
                var accessRight = e.Contains("accessright") ?
                    (int)e["accessright"] : -1;

                switch ((AccessRights)accessRight)
                {
                    case AccessRights.AppendAccess:
                        return "Append";
                    case AccessRights.AppendToAccess:
                        return "Append To";
                    case AccessRights.AssignAccess:
                        return "Assign";
                    case AccessRights.CreateAccess:
                        return "Create";
                    case AccessRights.DeleteAccess:
                        return "Delete";
                    case AccessRights.None:
                        return "None";
                    case AccessRights.ReadAccess:
                        return "Read";
                    case AccessRights.ShareAccess:
                        return "Share";
                    case AccessRights.WriteAccess:
                        return "Write";
                }

                return string.Empty;
            }
            catch
            {
                MessageBox.Show("Error encountered in GetAccessRight");
                return "ERROR";
            }
        }

        private string GetEntityName(Entity e)
        {
            try
            {
                var entityName = string.Empty;

                if (e.Contains("privilegeobjecttypecode.objecttypecode"))
                {
                    var alias = e.GetAttributeValue<AliasedValue>("privilegeobjecttypecode.objecttypecode");

                    entityName = alias != null ? alias.Value as string : string.Empty;
                }

                if (!string.IsNullOrEmpty(entityName))
                {
                    var entityMetadata = _entityMetadata
                        .Where(m => m.LogicalName == entityName)
                        .FirstOrDefault();

                    if (entityMetadata != null &&
                        entityMetadata.DisplayName != null &&
                        entityMetadata.DisplayName.UserLocalizedLabel != null)
                    {
                        entityName = entityMetadata.DisplayName.UserLocalizedLabel.Label;
                    }
                }

                return entityName;
            }
            catch
            {
                MessageBox.Show("Error encountered in GetEntityName");
                return "ERROR";
            }
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

        private async void ExportAll()
        {
            IsBusy = true;

            try
            {
                var temp = Path.GetTempPath();
                var tempFile = Path.Combine(temp, "allroles.csv");

                var results = await GetAll();

                await WriteAllRoles(results, tempFile);

                var dlg = new SaveFileDialog();
                dlg.FileName = "All Security Roles";
                dlg.DefaultExt = ".csv";
                dlg.Filter = "CSV Files |*.csv";

                var result = dlg.ShowDialog();

                if (result == true)
                {
                    var filename = dlg.FileName;

                    if (File.Exists(filename))
                    {
                        File.Delete(filename);
                    }

                    File.Move(tempFile, filename);

                    if (MessageBox.Show("Open the spreadsheet using your default editor?", string.Empty, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        Process.Start(filename);
                    }
                }
                else
                {
                    File.Delete(tempFile);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error was encountered while attempting to save an export of all roles. Detail: " + ex.Message);
            }

            IsBusy = false;
        }

        private Task WriteAllRoles(List<RoleExport> results, string tempFile)
        {
            return Task.Run(() =>
            {
                using (var sw = new StreamWriter(tempFile))
                {
                    var writer = new CsvWriter(sw);
                    writer.WriteField("Security Role");
                    writer.WriteField("Entity");
                    writer.WriteField("Access Right");
                    writer.WriteField("Privilege Name");
                    writer.WriteField("Privilege ID");

                    writer.NextRecord();

                    foreach (var record in results.OrderBy(c => c.RoleName))
                    {
                        writer.WriteField(record.RoleName);
                        writer.WriteField(record.EntityName);
                        writer.WriteField(record.AccessRight);
                        writer.WriteField(record.Depth);
                        writer.WriteField(record.PrivilegeName);
                        writer.WriteField(record.PrivilegeId);

                        writer.NextRecord();
                    }
                }
            });
        }

        private async void Export()
        {
            IsBusy = true;

            try
            {
                var temp = Path.GetTempPath();
                var tempFile = Path.Combine(temp, "comparison.csv");

                await WriteComparison(tempFile);

                var dlg = new SaveFileDialog();
                dlg.FileName = string.Format("Comparison of {0} vs {1}",
                    SelectedRole.Name,
                    SelectedSecondaryRole.Name);
                dlg.DefaultExt = ".csv";
                dlg.Filter = "CSV Files |*.csv";

                var result = dlg.ShowDialog();

                if (result == true)
                {
                    var filename = dlg.FileName;

                    if (File.Exists(filename))
                    {
                        File.Delete(filename);
                    }

                    File.Move(tempFile, filename);

                    if (MessageBox.Show("Open the spreadsheet using your default editor?", string.Empty, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        Process.Start(filename);
                    }
                }
                else
                {
                    File.Delete(tempFile);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error was encountered while attempting to save the comparison. Detail: " + ex.Message);
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
                    writer.WriteField("Privileges Match");
                    writer.WriteField("Entity");
                    writer.WriteField("Access Right");
                    writer.WriteField(SelectedRole.Name + " Privilege Depth");
                    writer.WriteField(SelectedSecondaryRole.Name + " Privilege Depth");
                    writer.WriteField("Privilege Name");
                    writer.WriteField("Privilege ID");

                    writer.NextRecord();

                    foreach (var record in Comparisons.OrderBy(c => c.Name))
                    {
                        writer.WriteField(record.IsMatchYesNo);
                        writer.WriteField(record.EntityName);
                        writer.WriteField(record.AccessRight);
                        writer.WriteField(record.Depth1);
                        writer.WriteField(record.Depth2);
                        writer.WriteField(record.Name);
                        writer.WriteField(record.PrivilegeId);

                        writer.NextRecord();
                    }
                }
            });
        }
    }
}