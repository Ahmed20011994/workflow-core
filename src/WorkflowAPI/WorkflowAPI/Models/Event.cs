using Newtonsoft.Json.Linq;

namespace WorkflowAPI.Models
{
    public class Event
    {
        public string WorkflowName { get; set; }
        public string EventName { get; set; }
        public string InstanceId { get; set; }
        public string DataClassName { get; set; }
        public JObject Data { get; set; }
    }
}