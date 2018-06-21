using System.Collections.Generic;

namespace WorkflowCore.Models.DefinitionStorage
{
    public class Notification
    {
        public string Operation { get; set; }
        public List<Recepient> Recepients { get; set; }
        public string Type { get; set; }
    }
}