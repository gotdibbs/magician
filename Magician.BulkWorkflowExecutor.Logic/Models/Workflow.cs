using System;

namespace Magician.BulkWorkflowExecutor.Logic.Models
{
    public class Workflow
    {
        public Guid WorkflowId { get; set; }

        public string Name { get; set; }

        public string LogicalName { get; set; }
    }
}
