using System.Collections.Generic;

namespace WorkflowCore.Models.DefinitionStorage
{
    public class Validation
    {
        public List<string> Fields { get; set; }
        public List<DataValidation> DataValidations { get; set; }
    }
}