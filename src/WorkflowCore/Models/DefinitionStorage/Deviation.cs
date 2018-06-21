using System.Collections.Generic;

namespace WorkflowCore.Models.DefinitionStorage
{
    public class Deviation
    {
        public string StepId { get; set; }
        public List<int> Roles { get; set; }
    }
}