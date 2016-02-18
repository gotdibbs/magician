using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magician.BulkWorkflowExecutor.Models
{
    public class ExecuteResponse
    {
        public string ErrorMessage { get; set; }

        public bool HasError { get; set; }

        public bool HasMoreResults { get; set; }

        public int ProcessedCount { get; set; }
    }
}
