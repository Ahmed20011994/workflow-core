using System;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Sample04.Steps;

namespace WorkflowCore.Sample04
{
    public class EventSampleWorkflow : IWorkflow<MyDataClass>
    {
        public string Id => "EventSampleWorkflow";
            
        public int Version => 1;
            
        public void Build(IWorkflowBuilder<MyDataClass> builder)
        {
            builder
                .StartWith(context => ExecutionResult.Next())
                .WaitFor("MyEvent", (data, context) => context.Workflow.Id, data => DateTime.Now)
                //.Output(data => data.StrValue, step => step.EventData)
                .Output(data => data.StrValue1, step => step.EventData)
                .Output(data => data.StrValue2, step => step.EventData)
                .Then<CustomMessage>()
                .Input(step => step.Message, data => "The data from the event is " + data.StrValue1 + " and " + data.StrValue2)
                .Then(context => Console.WriteLine("workflow complete"));
        }
    }
}