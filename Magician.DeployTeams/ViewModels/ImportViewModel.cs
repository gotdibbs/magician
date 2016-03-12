using GalaSoft.MvvmLight.Command;
using Magician.Connect;
using Magician.DeployTeams.Models;
using Microsoft.Win32;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
    public class ImportViewModel : ImportExportViewModelBase
    {
        public ICommand LoadCommand { get; private set; }
        public ICommand DeployCommand { get; private set; }

        public ImportViewModel()
        {
            ConnectCommand = new RelayCommand(() => Connect());

            LoadCommand = new RelayCommand(() => LoadExportData());
            DeployCommand = new RelayCommand(() => Deploy());
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

            await LoadExportData();

            IsConnected = true;
            IsBusy = false;
        }

        private async Task LoadExportData()
        {
            IsBusy = true;
            ProgressMessage = "Beginning data load to memory...";

            try
            {
                var dlg = new OpenFileDialog();
                dlg.DefaultExt = ".json";
                dlg.Filter = "JSON Files |*.json";

                var result = dlg.ShowDialog();

                if (result == true)
                {
                    ProgressMessage = "Loading exported data...";
                    var filename = dlg.FileName;

                    var teams = await DeserializeTeamsFromFile(filename);

                    Teams = new ObservableCollection<Team>(teams);
                }
                else
                {
                    MessageBox.Show("You must select a file to deploy.");
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error was encountered while attempting to load the export. Detail: " + ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private Task<List<Team>> DeserializeTeamsFromFile(string filePath)
        {
            return Task.Run(() =>
            {
                var text = File.ReadAllText(filePath);

                var data = (JArray)JsonConvert.DeserializeObject(text);

                return data.ToObject<List<Team>>();
            });
        }

        private async void Deploy()
        {
            try
            {
                var selected = Teams.Where(t => t.IsSelected == true).ToList();

                if (selected.Count < 1)
                {
                    MessageBox.Show("You must select at least one team to deploy.");
                    return;
                }

                IsBusy = true;

                ProgressMessage = "Locating team administrators...";
                await UpdateOwners(selected);

                ProgressMessage = "Locating business units...";
                await UpdateBusinessUnits(selected);

                ProgressMessage = "Locating currencies...";
                await UpdateCurrencies(selected);

                ProgressMessage = "Upserting teams...";
                await Upsert(selected);

                ProgressMessage = "Removing non-matching roles...";
                await RemoveRoles(selected);

                ProgressMessage = "Associating security roles...";
                await AssignRoles(selected);

                IsBusy = false;
            }
            catch
            {
                throw;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private Task UpdateOwners(List<Team> teams)
        {
            return Task.Run(() =>
            {
                foreach (var team in teams)
                {
                    var query = new QueryExpression("systemuser");
                    query.PageInfo = new PagingInfo();
                    query.PageInfo.Count = 1;
                    query.PageInfo.PageNumber = 1;
                    query.ColumnSet = new ColumnSet("systemuserid");
                    query.Criteria.AddCondition("domainname", ConditionOperator.Equal, team.AdministratorDomainName);

                    var result = _service.RetrieveMultiple(query);

                    if (result != null && result.Entities != null && result.Entities.Count == 1)
                    {
                        team.DestinationAdministratorId = result.Entities.First().Id;
                    }
                    else
                    {
                        throw new Exception("Could not locate a single user with domainname " + team.AdministratorDomainName);
                    }
                }
            });
        }

        private Task UpdateBusinessUnits(List<Team> teams)
        {
            return Task.Run(() =>
            {
                var query = new QueryExpression("businessunit");
                query.PageInfo = new PagingInfo();
                query.PageInfo.Count = 1;
                query.PageInfo.PageNumber = 1;
                query.ColumnSet = new ColumnSet("businessunitid");
                query.Criteria.AddCondition("parentbusinessunitid", ConditionOperator.Null);

                var result = _service.RetrieveMultiple(query);

                var rootBusinessUnitId = Guid.Empty;

                if (result != null && result.Entities != null && result.Entities.Count == 1)
                {
                    rootBusinessUnitId = result.Entities.First().Id;
                }
                else
                {
                    throw new Exception("Could not locate root business unit.");
                }

                foreach (var team in teams)
                {
                    if (team.IsInRootBusinessUnit)
                    {
                        team.DestinationBusinessUnitId = rootBusinessUnitId;
                        continue;
                    }

                    query = new QueryExpression("businessunit");
                    query.PageInfo = new PagingInfo();
                    query.PageInfo.Count = 1;
                    query.PageInfo.PageNumber = 1;
                    query.ColumnSet = new ColumnSet("businessunitid");
                    query.Criteria.AddCondition("name", ConditionOperator.Equal, team.BusinessUnit);

                    result = _service.RetrieveMultiple(query);

                    if (result != null && result.Entities != null && result.Entities.Count == 1)
                    {
                        team.DestinationBusinessUnitId = result.Entities.First().Id;
                    }
                    else
                    {
                        throw new Exception("Could not locate a single business unit with name " + team.BusinessUnit);
                    }
                }
            });
        }

        private Task UpdateCurrencies(List<Team> teams)
        {
            return Task.Run(() =>
            {
                foreach (var team in teams)
                {
                    if (string.IsNullOrEmpty(team.Currency))
                    {
                        continue;
                    }

                    var query = new QueryExpression("transactioncurrency");
                    query.PageInfo = new PagingInfo();
                    query.PageInfo.Count = 1;
                    query.PageInfo.PageNumber = 1;
                    query.ColumnSet = new ColumnSet("transactioncurrencyid");
                    query.Criteria.AddCondition("currencyname", ConditionOperator.Equal, team.Currency);

                    var result = _service.RetrieveMultiple(query);

                    if (result != null && result.Entities != null && result.Entities.Count == 1)
                    {
                        team.DestinationCurrencyId = result.Entities.First().Id;
                    }
                    else
                    {
                        throw new Exception("Could not locate a single currency with name " + team.Currency);
                    }
                }
            });
        }

        private Task Upsert(List<Team> teams)
        {
            return Task.Run(() =>
            {
                var response = (RetrieveEntityResponse)_service.Execute(new RetrieveEntityRequest
                {
                    LogicalName = "team",
                    EntityFilters = EntityFilters.Attributes
                });

                if (response == null || response.EntityMetadata == null)
                {
                    throw new Exception("Could not retrieve entity metadata for team.");
                }

                var metadata = response.EntityMetadata;

                foreach (var team in teams)
                {
                    var entity = new Entity("team");
                    entity["teamid"] = entity.Id = team.TeamId;
                    entity["businessunitid"] = new EntityReference("businessunit", team.DestinationBusinessUnitId);
                    entity["administratorid"] = new EntityReference("systemuser", team.DestinationAdministratorId);
                    entity["transactioncurrencyid"] = new EntityReference("transactioncurrency", team.DestinationCurrencyId);

                    foreach (var attribute in team.Attributes)
                    {
                        var type = attribute.Value.GetType();

                        if (type != typeof(string) && type != typeof(decimal?) && type != typeof(int?) &&
                            type != typeof(double?) && type != typeof(DateTime?))
                        {
                            var am = metadata.Attributes.Where(a => a.LogicalName == attribute.Key).FirstOrDefault();

                            if (am == null)
                            {
                                throw new Exception("Could not locate entity metadata for team:" + attribute.Key);
                            }

                            var attributeType = GetAttributeType(am.AttributeType);

                            if (attributeType == null)
                            {
                                throw new Exception("Could not determine the type of team:" + attribute.Key);
                            }

                            object value = ((JToken)attribute.Value).ToObject(attributeType);

                            entity.Attributes.Add(attribute.Key, value);
                        }
                        else
                        {
                            entity.Attributes.Add(attribute.Key, attribute.Value);
                        }
                    }

                    _service.Execute(new UpsertRequest
                    {
                        Target = entity
                    });
                }
            });
        }

        private Type GetAttributeType(AttributeTypeCode? attributeType)
        {
            if (attributeType == null)
            {
                return null;
            }

            switch (attributeType)
            {
                case AttributeTypeCode.Customer:
                case AttributeTypeCode.Lookup:
                case AttributeTypeCode.Owner:
                    return typeof(EntityReference);
                case AttributeTypeCode.Money:
                    return typeof(Money);
                case AttributeTypeCode.Picklist:
                case AttributeTypeCode.State:
                case AttributeTypeCode.Status:
                    return typeof(OptionSetValue);
                default:
                    return null;
            }
        }

        private Task RemoveRoles(List<Team> teams)
        {
            return Task.Run(() =>
            {
                foreach (var team in teams)
                {
                    var query = new QueryExpression("role");
                    query.Distinct = true;
                    query.ColumnSet = new ColumnSet("roleid");
                    query.Criteria.AddCondition("name", ConditionOperator.NotIn, team.SecurityRoles.ToArray());
                    query.Criteria.AddCondition("businessunitid", ConditionOperator.Equal, team.DestinationBusinessUnitId);

                    var teamLink = query.AddLink("teamroles", "roleid", "roleid", JoinOperator.Inner);
                    teamLink.LinkCriteria.AddCondition("teamid", ConditionOperator.Equal, team.TeamId);

                    var result = _service.RetrieveMultiple(query);

                    if (result == null || result.Entities == null || result.Entities.Count == 0)
                    {
                        // Nothing to remove
                        continue;
                    }

                    var roles = result.Entities
                        .Select(e => new EntityReference("role", e.Id))
                        .ToList();

                    _service.Disassociate("team", team.TeamId,
                        new Relationship("teamroles_association"),
                        new EntityReferenceCollection(roles));
                }
            });
        }

        private Task AssignRoles(List<Team> teams)
        {
            return Task.Run(() =>
            {
                foreach (var team in teams)
                {
                    var query = new QueryExpression("role");
                    query.Distinct = true;
                    query.ColumnSet = new ColumnSet("roleid");
                    query.Criteria.AddCondition("name", ConditionOperator.In, team.SecurityRoles.ToArray());
                    query.Criteria.AddCondition("businessunitid", ConditionOperator.Equal, team.DestinationBusinessUnitId);

                    var result = _service.RetrieveMultiple(query);

                    if (result == null || result.Entities == null && result.Entities.Count != team.SecurityRolesCount)
                    {
                        throw new Exception("Could not find all security roles in the destination for team with name: " + team.Name);
                    }

                    var roles = result.Entities
                        .Select(e => new EntityReference("role", e.Id))
                        .ToList();

                    _service.Associate("team", team.TeamId,
                        new Relationship("teamroles_association"),
                        new EntityReferenceCollection(roles));
                }
            });
        }
    }
}
