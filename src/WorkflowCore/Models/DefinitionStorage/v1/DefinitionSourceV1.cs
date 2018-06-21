using System;
using System.Collections.Generic;

namespace WorkflowCore.Models.DefinitionStorage.v1
{
    public class DefinitionSourceV1 : DefinitionSource
    {
        public string DataType { get; set; }

        public List<int> Creators { get; set; }

        public List<Notification> Notifications { get; set; }

        public List<Escalation> Escalations { get; set; }

        public WorkflowErrorHandling DefaultErrorBehavior { get; set; }

        public TimeSpan? DefaultErrorRetryInterval { get; set; }

        public List<StepSourceV1> Steps { get; set; } = new List<StepSourceV1>();
    }
}