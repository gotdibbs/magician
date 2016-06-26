namespace Magician.BulkWorkflowExecutor.Logic.Models
{
    public class ExecuteResponse
    {
        public string ErrorMessage { get; set; }

        public bool HasError { get; set; }

        public bool HasMoreResults { get; set; }

        public int ProcessedCount { get; set; }
    }
}
