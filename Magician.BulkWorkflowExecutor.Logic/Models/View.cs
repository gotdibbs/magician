using System;

namespace Magician.BulkWorkflowExecutor.Logic.Models
{
    public class View
    {
        public Guid ViewId { get; set; }

        public string Name { get; set; }

        public string FetchXml { get; set; }
    }
}
