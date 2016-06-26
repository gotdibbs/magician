using Magician.BulkWorkflowExecutor.Logic.Models;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magician.BulkWorkflowExecutor.Logic
{
    public class ExecuteBulkWorkflowLogic
    {
        public IOrganizationService OrganizationService { get; set; }

        public ExecuteBulkWorkflowLogic(IOrganizationService service)
        {
            OrganizationService = service;
        }

        public Workflow RetrieveWorkflow(Guid workflowId)
        {
            var workflow = OrganizationService.Retrieve(
                "workflow",
                workflowId,
                new ColumnSet("name", "workflowid", "primaryentity"));

            return new Workflow
            {
                WorkflowId = workflow.Id,
                Name = workflow["name"] as string,
                LogicalName = workflow["primaryentity"] as string
            };
        }

        public Workflow RetrieveWorkflow(string name)
        {
            var query = new QueryExpression("workflow");

            query.PageInfo = new PagingInfo();
            query.PageInfo.Count = 1;
            query.PageInfo.PageNumber = 1;

            query.NoLock = true;
            query.Distinct = true;

            query.ColumnSet = new ColumnSet("name", "workflowid", "primaryentity");

            query.Criteria.AddCondition("ondemand", ConditionOperator.Equal, true);
            query.Criteria.AddCondition("primaryentity", ConditionOperator.NotNull);
            query.Criteria.AddCondition("statecode", ConditionOperator.Equal, 1); // active
            query.Criteria.AddCondition("statuscode", ConditionOperator.Equal, 2); // published
            query.Criteria.AddCondition("type", ConditionOperator.Equal, 1); // definition
            query.Criteria.AddCondition("name", ConditionOperator.Equal, name);

            var result = OrganizationService.RetrieveMultiple(query);

            return result.Entities.Select(e =>
            {
                return new Workflow
                {
                    WorkflowId = e.Id,
                    Name = e["name"] as string,
                    LogicalName = e["primaryentity"] as string
                };
            }).FirstOrDefault();
        }

        public List<Workflow> RetrieveWorkflows()
        {
            var query = new QueryExpression("workflow");
            query.NoLock = true;
            query.Distinct = true;
            query.ColumnSet = new ColumnSet("name", "workflowid", "primaryentity");
            query.Criteria.AddCondition("ondemand", ConditionOperator.Equal, true);
            query.Criteria.AddCondition("primaryentity", ConditionOperator.NotNull);
            query.Criteria.AddCondition("statecode", ConditionOperator.Equal, 1); // active
            query.Criteria.AddCondition("statuscode", ConditionOperator.Equal, 2); // published
            query.Criteria.AddCondition("type", ConditionOperator.Equal, 1); // definition

            var result = OrganizationService.RetrieveMultiple(query);

            return result.Entities.Select(e =>
            {
                return new Workflow
                {
                    WorkflowId = e.Id,
                    Name = e["name"] as string,
                    LogicalName = e["primaryentity"] as string
                };
            }).OrderBy(e => e.Name).ToList();
        }

        public View RetrieveView(Guid viewId, string entityLogicalName, bool isUserView)
        {
            var view = OrganizationService.Retrieve(
                isUserView ? "userquery" : "savedquery",
                viewId,
                new ColumnSet("name", "fetchxml", isUserView ? "userqueryid" : "savedqueryid"));

            if (view != null)
            {
                return new View
                {
                    ViewId = view.Id,
                    Name = view["name"] as string,
                    FetchXml = view["fetchxml"] as string
                };
            }

            return null;
        }

        public View RetrieveView(string viewName, string entityLogicalName, bool isUserView)
        {
            var query = new QueryExpression(isUserView ? "userquery" : "savedquery");
            query.ColumnSet = new ColumnSet(isUserView ? "userqueryid" : "savedqueryid");
            query.Criteria.AddCondition("returnedtypecode", ConditionOperator.Equal, entityLogicalName);
            query.Criteria.AddCondition("name", ConditionOperator.Equal, viewName);

            return RetrieveView(query);
        }

        private View RetrieveView(QueryExpression query)
        {
            query.PageInfo = new PagingInfo();
            query.PageInfo.Count = 1;
            query.PageInfo.PageNumber = 1;

            query.NoLock = true;

            query.ColumnSet.AddColumns("name", "fetchxml");

            query.Criteria.AddCondition("querytype", ConditionOperator.Equal, 0);
            query.Criteria.AddCondition("fetchxml", ConditionOperator.NotNull);
            query.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0); //active

            var results = OrganizationService.RetrieveMultiple(query);

            return results.Entities.Select(e =>
            {
                return new View
                {
                    ViewId = e.Id,
                    Name = e["name"] as string,
                    FetchXml = e["fetchxml"] as string
                };
            }).FirstOrDefault();
        }

        public List<View> RetrieveViews(string entityLogicalName)
        {
            var query = new QueryExpression("savedquery");
            query.NoLock = true;
            query.ColumnSet = new ColumnSet("name", "savedqueryid", "fetchxml");
            query.Criteria.AddCondition("querytype", ConditionOperator.Equal, 0);
            query.Criteria.AddCondition("returnedtypecode", ConditionOperator.Equal, entityLogicalName);
            query.Criteria.AddCondition("fetchxml", ConditionOperator.NotNull);
            query.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0); //active

            var results = OrganizationService.RetrieveMultiple(query);

            var views = results.Entities.Select(e =>
            {
                return new View
                {
                    ViewId = e.Id,
                    Name = e["name"] as string,
                    FetchXml = e["fetchxml"] as string
                };
            }).ToList();

            query = new QueryExpression("userquery");
            query.NoLock = true;
            query.ColumnSet = new ColumnSet("name", "userqueryid", "fetchxml");
            query.Criteria.AddCondition("querytype", ConditionOperator.Equal, 0);
            query.Criteria.AddCondition("returnedtypecode", ConditionOperator.Equal, entityLogicalName);
            query.Criteria.AddCondition("fetchxml", ConditionOperator.NotNull);
            query.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0); //active

            results = OrganizationService.RetrieveMultiple(query);

            views.AddRange(results.Entities.Select(e =>
            {
                return new View
                {
                    ViewId = e.Id,
                    Name = e["name"] as string,
                    FetchXml = e["fetchxml"] as string
                };
            }));

            views.Sort(delegate (View v1, View v2) { return v1.Name.CompareTo(v2.Name); });

            return views;
        }

        public ExecuteResponse Execute(QueryExpression query, Guid workflowId, int page = 1)
        {
            return ExecuteWorkflow(query, workflowId, page);
        }

        public QueryExpression GetQuery(string fetchXml)
        {
            var request = new FetchXmlToQueryExpressionRequest();
            request.FetchXml = fetchXml;
            var response = (FetchXmlToQueryExpressionResponse)OrganizationService.Execute(request);

            return response.Query;
        }

        private ExecuteResponse ExecuteWorkflow(QueryExpression query, Guid workflowId, int page)
        {
            var response = new ExecuteResponse();

            try
            {
                query.PageInfo.Count = 100;
                query.PageInfo.PageNumber = page;

                var result = OrganizationService.RetrieveMultiple(query);

                response.HasMoreResults = result.MoreRecords;
                response.ProcessedCount = result.Entities.Count;

                var em = new ExecuteMultipleRequest
                {
                    Settings = new ExecuteMultipleSettings
                    {
                        ContinueOnError = false,
                        ReturnResponses = true
                    }
                };

                em.Requests = new OrganizationRequestCollection();
                em.Requests.AddRange(result.Entities.Select(e => new ExecuteWorkflowRequest
                {
                    EntityId = e.Id,
                    WorkflowId = workflowId
                }));

                var emResponse = (ExecuteMultipleResponse)OrganizationService.Execute(em);

                if (emResponse.IsFaulted)
                {
                    var firstFault = emResponse.Responses.Where(r => r.Fault != null)
                        .FirstOrDefault();

                    response.HasError = true;
                    response.ErrorMessage = firstFault != null ? firstFault.Fault.Message : "Unknown execute workflow error";

                    return response;
                }
            }
            catch (Exception ex)
            {
                response.HasError = true;
                response.ErrorMessage = ex.Message;
            }

            return response;
        }
    }
}
