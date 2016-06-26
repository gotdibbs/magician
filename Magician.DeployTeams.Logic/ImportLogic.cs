using Magician.DeployTeams.Logic.Models;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Magician.DeployTeams.Logic
{
    public class ImportLogic
    {
        private List<string> _attributesToSkip = new List<string>
        {
            "createdon", "createdby", "modifiedon", "modifiedby", "ownerid",
            "isdefault", "systemmanaged", "queueid", "organizationid",
            "modifiedonbehalfby", "exchangerate", "transactioncurrencyid",
            "businessunitid", "administratorid", "teamid", "administratorid.domainname"
        };

        public IOrganizationService OrganizationService { get; set; }

        public ImportLogic(IOrganizationService service)
        {
            OrganizationService = service;
        }

        public List<Team> DeserializeTeamsFromFile(string filePath)
        {
            var text = File.ReadAllText(filePath);

            var data = (JArray)JsonConvert.DeserializeObject(text);

            return data.ToObject<List<Team>>();
        }

        public void Import(List<Team> teams)
        {
            if (teams == null || teams.Count < 1)
            {
                throw new ArgumentOutOfRangeException("You must select at least one team to deploy.");
            }

            UpdateBusinessUnits(teams);

            foreach (var team in teams)
            {
                SetOwner(team);
                SetCurrency(team);
            }

            Upsert(teams);

            foreach (var team in teams)
            {
                RemoveRoles(team);
                AssignRoles(team);
            }
        }

        private void UpdateBusinessUnits(List<Team> teams)
        {
            var query = new QueryExpression("businessunit");
            query.PageInfo = new PagingInfo();
            query.PageInfo.Count = 1;
            query.PageInfo.PageNumber = 1;
            query.ColumnSet = new ColumnSet("businessunitid");
            query.Criteria.AddCondition("parentbusinessunitid", ConditionOperator.Null);

            var result = OrganizationService.RetrieveMultiple(query);

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

                result = OrganizationService.RetrieveMultiple(query);

                if (result != null && result.Entities != null && result.Entities.Count == 1)
                {
                    team.DestinationBusinessUnitId = result.Entities.First().Id;
                }
                else
                {
                    throw new Exception("Could not locate a single business unit with name " + team.BusinessUnit);
                }
            }
        }

        private void SetOwner(Team team)
        {
            var query = new QueryExpression("systemuser");
            query.PageInfo = new PagingInfo();
            query.PageInfo.Count = 1;
            query.PageInfo.PageNumber = 1;
            query.ColumnSet = new ColumnSet("systemuserid");
            query.Criteria.AddCondition("domainname", ConditionOperator.Equal, team.AdministratorDomainName);

            var result = OrganizationService.RetrieveMultiple(query);

            if (result != null && result.Entities != null && result.Entities.Count == 1)
            {
                team.DestinationAdministratorId = result.Entities.First().Id;
            }
            else
            {
                throw new Exception("Could not locate a single user with domainname " + team.AdministratorDomainName);
            }
        }

        private void SetCurrency(Team team)
        {
            if (string.IsNullOrEmpty(team.Currency))
            {
                return;
            }

            var query = new QueryExpression("transactioncurrency");
            query.PageInfo = new PagingInfo();
            query.PageInfo.Count = 1;
            query.PageInfo.PageNumber = 1;
            query.ColumnSet = new ColumnSet("transactioncurrencyid");
            query.Criteria.AddCondition("currencyname", ConditionOperator.Equal, team.Currency);

            var result = OrganizationService.RetrieveMultiple(query);

            if (result != null && result.Entities != null && result.Entities.Count == 1)
            {
                team.DestinationCurrencyId = result.Entities.First().Id;
            }
            else
            {
                throw new Exception("Could not locate a single currency with name " + team.Currency);
            }
        }

        private void Upsert(List<Team> teams)
        {
            var response = (RetrieveEntityResponse)OrganizationService.Execute(new RetrieveEntityRequest
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

                OrganizationService.Execute(new UpsertRequest
                {
                    Target = entity
                });
            }
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

        private void RemoveRoles(Team team)
        {
            var query = new QueryExpression("role");
            query.Distinct = true;
            query.ColumnSet = new ColumnSet("roleid");

            if (team.SecurityRoles != null && team.SecurityRoles.Count > 0)
            {
                query.Criteria.AddCondition("name", ConditionOperator.NotIn, team.SecurityRoles.ToArray());
            }

            query.Criteria.AddCondition("businessunitid", ConditionOperator.Equal, team.DestinationBusinessUnitId);

            var teamLink = query.AddLink("teamroles", "roleid", "roleid", JoinOperator.Inner);
            teamLink.LinkCriteria.AddCondition("teamid", ConditionOperator.Equal, team.TeamId);

            var result = OrganizationService.RetrieveMultiple(query);

            if (result == null || result.Entities == null || result.Entities.Count == 0)
            {
                // Nothing to remove
                return;
            }

            var roles = result.Entities
                .Select(e => new EntityReference("role", e.Id))
                .ToList();

            OrganizationService.Disassociate("team", team.TeamId,
                new Relationship("teamroles_association"),
                new EntityReferenceCollection(roles));
        }

        private void AssignRoles(Team team)
        {
            if (team.SecurityRoles == null || team.SecurityRoles.Count == 0)
            {
                return;
            }

            var query = new QueryExpression("role");
            query.Distinct = true;
            query.ColumnSet = new ColumnSet("roleid");
            query.Criteria.AddCondition("name", ConditionOperator.In, team.SecurityRoles.ToArray());
            query.Criteria.AddCondition("businessunitid", ConditionOperator.Equal, team.DestinationBusinessUnitId);

            var result = OrganizationService.RetrieveMultiple(query);

            if (result == null || result.Entities == null && result.Entities.Count != team.SecurityRolesCount)
            {
                throw new Exception("Could not find all security roles in the destination for team with name: " + team.Name);
            }

            var roles = result.Entities
                .Select(e => new EntityReference("role", e.Id))
                .ToList();

            OrganizationService.Associate("team", team.TeamId,
                new Relationship("teamroles_association"),
                new EntityReferenceCollection(roles));
        }
    }
}
