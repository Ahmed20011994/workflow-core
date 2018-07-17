using System.Collections.Generic;

namespace WorkflowCore.Models.DefinitionStorage
{
    public class DataValidation
    {
        public string Name { get; set; }
        public List<int> Discrepants { get; set; }
        public string Input { get; set; }
        public string ErrorCode { get; set; }
    }
}