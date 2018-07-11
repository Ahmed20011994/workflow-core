using System;

namespace WorkflowAPI.Models
{
    public class FlowModel
    {
        public string WorkflowId { get; set; }

        public string WorkflowName { get; set; }

        public string Description { get; set; }

        public string Definition { get; set; }

        public DateTime CreationTime { get; set; }
    }
}