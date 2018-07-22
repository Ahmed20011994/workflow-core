using System.Collections.Generic;

namespace WorkflowAPI.Output
{
    public class WorkflowOutput
    {
        public string InstanceId { get; set; }
        public string WorkflowStatus { get; set; }
        public string LastStepName { get; set; }
        public string StepStatus { get; set; }
        public string EventName { get; set; }
        public List<ExpectedInput> ExpectedInputs { get; set; }
    }
}