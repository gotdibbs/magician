using Magician.DeployTeams.Logic.Models;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magician.DeployTeams.Logic
{
    public class ExportLogic
    {
        private List<string> _attributesToSkip = new List<string>
        {
            "createdon", "createdby", "modifiedon", "modifiedby", "ownerid",
            "isdefault", "systemmanaged", "queueid", "organizationid",
            "modifiedonbehalfby", "exchangerate", "transactioncurrencyid",
            "businessunitid", "administratorid", "teamid", "administratorid.domainname"
        };

        public IOrganizationService OrganizationService { get; set; }

        public ExportLogic(IOrganizationService service)
        {
            OrganizationService = service;
        }

        public List<Team> RetrieveTeams()
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

            var result = OrganizationService.RetrieveMultiple(query);

            return result.Entities.Select(e =>
            {
                var roles = LoadSecurityRoles(e.Id);

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
                        .ToDictionary(a => a.Key, a => a.Value),
                    SecurityRoles = roles
                };
            }).OrderBy(e => e.Name).ToList();
        }

        private List<string> LoadSecurityRoles(Guid teamId)
        {
            var query = new QueryExpression("role");
            query.NoLock = true;
            query.Distinct = true;
            query.ColumnSet = new ColumnSet("name");

            var teamroles = query.AddLink("teamroles", "roleid", "roleid", JoinOperator.Inner);
            teamroles.LinkCriteria.AddCondition("teamid", ConditionOperator.Equal, teamId);

            var result = OrganizationService.RetrieveMultiple(query);

            return result.Entities
                .Select(e => e["name"] as string)
                .ToList();
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

        private string GetNameFromEntityReference(Entity e, string attribute)
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
                throw new Exception("Error encountered in GetNameFromEntityReference for attribute " + attribute);
            }
        }

        public void Export(string exportLocation, IEnumerable<Team> teams)
        {
            try
            {
                if (File.Exists(exportLocation))
                {
                    File.Delete(exportLocation);
                }

                SerializeTeamsToFile(exportLocation, teams);
            }
            catch (Exception ex)
            {
                throw new Exception("An error was encountered while attempting to save the export. Detail: " + ex.Message);
            }
        }

        private void SerializeTeamsToFile(string filePath, IEnumerable<Team> teams)
        {
            var serializedTeams = JsonConvert.SerializeObject(teams);

            File.WriteAllText(filePath, serializedTeams);
        }
    }
}
