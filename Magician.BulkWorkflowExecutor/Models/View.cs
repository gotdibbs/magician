using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magician.BulkWorkflowExecutor.Models
{
    public class View
    {
        public Guid ViewId { get; set; }

        public string Name { get; set; }

        public string FetchXml { get; set; }
    }
}
