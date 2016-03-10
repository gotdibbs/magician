using GalaSoft.MvvmLight.Command;
using Magician.Connect;
using Magician.DeployTeams.Models;
using Microsoft.Win32;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Magician.DeployTeams.ViewModels
{
    public class ExportViewModel : ImportExportViewModelBase
    {
        private List<string> _attributesToSkip = new List<string>
        {
            "createdon", "createdby", "modifiedon", "modifiedby", "ownerid",
            "isdefault", "systemmanaged", "queueid", "organizationid",
            "modifiedonbehalfby", "exchangerate", "transactioncurrencyid",
            "businessunitid", "administratorid", "teamid", "administratorid.domainname"
        };

        public ICommand ExportCommand { get; private set; }

        public ExportViewModel()
        {
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
                MessageBox.Show("Please click Connect to try connecting to Dynamics CRM again. A valid connection is required.");
                return;
            }

            IsBusy = true;
            IsConnected = false;

            _service = _connector.OrganizationServiceProxy;

            ConnectText = "Change from " + _connector.OrganizationFriendlyName;

            ProgressMessage = "Retrieving teams...";
            var teams = await RetrieveTeams();
            ProgressMessage = "Retrieiving security roles...";
            await LoadSecurityRoles(teams);

            Teams = new ObservableCollection<Team>(teams);

            IsConnected = true;
            IsBusy = false;
        }


        private Task<List<Team>> RetrieveTeams()
        {
            return Task.Run(() =>
            {
                var query = new QueryExpression("team");
                query.NoLock = true;
                query.Distinct = true;
                query.ColumnSet = new ColumnSet(true);
                query.Criteria.AddCondition("isdefault", ConditionOperator.Equal, false);
                query.Criteria.AddCondition("systemmanaged", ConditionOperator.Equal, false);

                var bu = query.AddLink("businessunit", "businessunitid", "businessunitid", JoinOperator.Inner);
                bu.EntityAlias = "businessunitid";
                bu.Columns = new ColumnSet("parentbusinessunitid");

                var admin = query.AddLink("systemuser", "administratorid", "systemuserid", JoinOperator.LeftOuter);
                admin.EntityAlias = "administratorid";
                admin.Columns = new ColumnSet("domainname");

                var result = _service.RetrieveMultiple(query);

                return result.Entities.Select(e =>
                {
                    return new Team
                    {
                        TeamId = e.Id,
                        Name = e["name"] as string,
                        Type = e.FormattedValues["teamtype"],
                        IsInRootBusinessUnit = GetIsUnderDefaultBusinessUnit(e),
                        BusinessUnit = GetNameFromEntityReference(e, "businessunitid"),
                        AdministratorDomainName = GetAdministrator(e),
                        AdministratorFullName = GetNameFromEntityReference(e, "administratorid"),
                        Currency = GetNameFromEntityReference(e, "transactioncurrencyid"),
                        Attributes = e.Attributes
                            .Where(a => !_attributesToSkip.Contains(a.Key))
                            .ToDictionary(a => a.Key, a => a.Value)
                    };
                }).OrderBy(e => e.Name).ToList();
            });
        }

        private Task LoadSecurityRoles(List<Team> teams)
        {
            return Task.Run(() =>
            {
                foreach (var team in teams)
                {
                    var query = new QueryExpression("role");
                    query.NoLock = true;
                    query.Distinct = true;
                    query.ColumnSet = new ColumnSet("name");

                    var teamroles = query.AddLink("teamroles", "roleid", "roleid", JoinOperator.Inner);
                    teamroles.LinkCriteria.AddCondition("teamid", ConditionOperator.Equal, team.TeamId);

                    var result = _service.RetrieveMultiple(query);

                    team.SecurityRoles = result.Entities
                        .Select(e => e["name"] as string)
                        .ToList();
                }
            });
        }

        private bool GetIsUnderDefaultBusinessUnit(Entity e)
        {
            try
            {
                if (e.Contains("businessunitid.parentbusinessunitid"))
                {
                    var er = e.GetAttributeValue<AliasedValue>("businessunitid.parentbusinessunitid");

                    if (er != null && er.Value != null)
                    {
                        return false;
                    }
                }

                return true;
            }
            catch
            {
                throw new Exception("Error encountered in GetIsUnderDefaultBusinessUnit");
            }
        }

        private string GetAdministrator(Entity e)
        {
            try
            {
                if (e.Contains("administratorid.domainname"))
                {
                    var er = e.GetAttributeValue<AliasedValue>("administratorid.domainname");

                    if (er != null && er.Value != null)
                    {
                        return er.Value as string;
                    }
                }

                return null;
            }
            catch
            {
                throw new Exception("Error encountered in GetAdministrator");
            }
        }

        public string GetNameFromEntityReference(Entity e, string attribute)
        {
            try
            {
                var name = string.Empty;

                if (e.Contains(attribute))
                {
                    var er = e.GetAttributeValue<EntityReference>(attribute);

                    name = er != null ? er.Name : string.Empty;
                }

                return name;
            }
            catch
            {
                MessageBox.Show("Error encountered in GetNameFromEntityReference for attribute " + attribute);
                return "ERROR";
            }
        }

        private async void Export()
        {
            IsBusy = true;
            ProgressMessage = "Beginning export...";

            try
            {
                var temp = Path.GetTempPath();
                var tempFile = Path.Combine(temp, "export.json");

                ProgressMessage = "Saving exported data...";
                await SerializeTeamsToFile(tempFile);

                var dlg = new SaveFileDialog();
                dlg.FileName = string.Format("{0:yyyy-MM-dd} - Team Export from {1}",
                    DateTime.Now,
                    _connector.OrganizationFriendlyName);
                dlg.DefaultExt = ".json";
                dlg.Filter = "JSON Files |*.json";

                var result = dlg.ShowDialog();

                if (result == true)
                {
                    ProgressMessage = "Moving exported data to destination...";
                    var filename = dlg.FileName;

                    if (File.Exists(filename))
                    {
                        File.Delete(filename);
                    }

                    File.Move(tempFile, filename);
                }
                else
                {
                    ProgressMessage = "Removing temporary export file...";
                    File.Delete(tempFile);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error was encountered while attempting to save the export. Detail: " + ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private Task SerializeTeamsToFile(string filePath)
        {
            return Task.Run(() =>
            {
                var serializedTeams = JsonConvert.SerializeObject(Teams);

                File.WriteAllText(filePath, serializedTeams);
            });
        }
    }
}
