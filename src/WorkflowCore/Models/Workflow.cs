using System;
using WorkflowCore.Models.DefinitionStorage.v1;

namespace WorkflowCore.Models
{
    public class Workflow
    {
        public string WorkflowId { get; set; }

        public string WorkflowName { get; set; }

        public string Description { get; set; }

        public DefinitionSourceV1 Definition { get; set; }

        public DateTime CreationTime { get; set; }
    }
}