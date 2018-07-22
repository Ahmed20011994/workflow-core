using Newtonsoft.Json.Linq;

namespace WorkflowAPI.Models
{
    public class Event
    {
        public string EventName { get; set; }
        public string InstanceId { get; set; }
        public JObject Data { get; set; }
    }
}