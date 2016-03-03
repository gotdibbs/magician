using Magician.WebResourceLiveSync.Model;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magician.WebResourceLiveSync.Data
{
    public class CrmService
    {
        private IOrganizationService _service;

        public CrmService(IOrganizationService service)
        {
            _service = service;
        }

        public IEnumerable<Entity> SearchWebResourcesByName(params string[] names)
        {
            var query = new QueryExpression("webresource");
            query.PageInfo = new PagingInfo
            {
                Count = 2,
                PageNumber = 1
            };
            query.ColumnSet = new ColumnSet("webresourceid", "content", "modifiedon");
            query.Criteria.FilterOperator = LogicalOperator.Or;

            foreach (var name in names)
            {
                query.Criteria.AddCondition("name", ConditionOperator.Equal, name);
            }

            var result = _service.RetrieveMultiple(query);

            if (result != null && result.Entities != null && result.Entities.Count > 0)
            {
                return result.Entities;
            }

            return Enumerable.Empty<Entity>();
        }

        public IEnumerable<Solution> RetrieveSolutions()
        {
            var query = new QueryExpression("solution");
            query.NoLock = true;
            query.ColumnSet = new ColumnSet("uniquename", "solutionid", "publisherid");
            query.Criteria.AddCondition("ismanaged", ConditionOperator.Equal, false);
            query.Criteria.AddCondition("uniquename", ConditionOperator.NotIn, new string[] { "Active", "Basic" });
            query.AddOrder("uniquename", OrderType.Ascending);
            var publisher = query.AddLink("publisher", "publisherid", "publisherid", JoinOperator.Inner);
            publisher.EntityAlias = "publisher";
            publisher.Columns = new ColumnSet("customizationprefix");

            var result = _service.RetrieveMultiple(query);

            var solutions = result.Entities.Select(e => new Solution
            {
                SolutionId = e.Id,
                Name = e["uniquename"] as string,
                PublisherId = ParsePublisherId(e),
                CustomizationPrefix = ParseCustomizationPrefix(e)
            });

            return solutions.OrderBy(r => r.Name);
        }

        private Guid ParsePublisherId(Entity e)
        {
            if (e.Contains("publisherid"))
            {
                var er = e.GetAttributeValue<EntityReference>("publisherid");

                if (er != null)
                {
                    return er.Id;
                }
            }

            throw new Exception(string.Format("Could not locate the publisher for solution {0}.", e["uniquename"]));
        }

        private string ParseCustomizationPrefix(Entity e)
        {
            var prefix = string.Empty;

            if (e.Contains("publisher.customizationprefix"))
            {
                var alias = e.GetAttributeValue<AliasedValue>("publisher.customizationprefix");

                prefix = alias != null ? alias.Value as string : string.Empty;
            }

            if (string.IsNullOrEmpty(prefix))
            {
                throw new Exception(string.Format("Could not parse customization prefix for solution {0}.", e["uniquename"]));
            }

            return prefix;
        }
    }
}
