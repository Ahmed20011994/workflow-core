using System;
using System.Collections.Generic;
using WorkflowCore.Models.DefinitionStorage;


namespace WorkflowCore.Models
{
    public class   WorkflowDefinition
    {
        public string Id { get; set; }

        public int Version { get; set; }

        public string Description { get; set; }
        public List<int> Creators { get; set; }

        public List<Notification> Notifications { get; set; }

        public List<Escalation> Escalations { get; set; }

        public List<WorkflowStep> Steps { get; set; }

        public Type DataType { get; set; }

        public WorkflowErrorHandling DefaultErrorBehavior { get; set; }

        public TimeSpan? DefaultErrorRetryInterval { get; set; }                

    }

    public enum WorkflowErrorHandling 
    { 
        Retry = 0, 
        Suspend = 1, 
        Terminate = 2,
        Compensate = 3
    }
}
