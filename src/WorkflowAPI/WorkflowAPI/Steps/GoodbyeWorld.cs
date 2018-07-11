using System.IO;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowAPI.Steps
{
    public class GoodbyeWorld : StepBody
    {
        public override ExecutionResult Run(IStepExecutionContext context)
        {
            using (StreamWriter writer = File.AppendText(@"E:\Workflow\SampleWorkflowExecution.txt"))
            {
                writer.WriteLine("Goodbye world");
            }
            return ExecutionResult.Next();
        }
    }
}