using System;
using System.ComponentModel.DataAnnotations;

namespace WorkflowCore.Persistence.EntityFramework.Models
{
    public class PersistedRegistry
    {
        [Key]
        public long WorkflowId { get; set; }

        public string WorkflowName { get; set; }

        public string Description { get; set; }

        public string Definition { get; set; }

        public DateTime CreationTime { get; set; }
    }
}