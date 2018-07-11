using System.IO;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowAPI.Steps
{
    public class PrintMessage : StepBody
    {
        public string Message { get; set; }

        public override ExecutionResult Run(IStepExecutionContext context)
        {
            using (StreamWriter writer = File.AppendText(@"E:\Workflow\SampleWorkflowExecution.txt"))
            {
                writer.WriteLine(Message);
            }
            return ExecutionResult.Next();
        }
    }
}