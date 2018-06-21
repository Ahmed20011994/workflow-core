using System.Collections.Generic;

namespace WorkflowCore.Models.DefinitionStorage
{
    public class Recepient
    {
        public string Name { get; set; }
        public List<string> Channels { get; set; }
        public string EmailAddress { get; set; }
        public string PhoneNumber { get; set; }
    }
}