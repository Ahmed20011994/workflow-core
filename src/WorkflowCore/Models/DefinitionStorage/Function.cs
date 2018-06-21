using System.Collections.Generic;

namespace WorkflowCore.Models.DefinitionStorage
{
    public class Function
    {
        public string Name { get; set; }
        public List<int> Discrepants { get; set; }
    }
}