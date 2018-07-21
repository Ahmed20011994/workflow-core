using System;
using System.ComponentModel.DataAnnotations;

namespace WorkflowCore.Persistence.EntityFramework.Models
{
    public class PersistedValidation
    {
        [Key]
        public long PersistenceId { get; set; }
        public string StepId { get; set; }
        public string ValidationName { get; set; }
        public string FieldName { get; set; }
        public string FieldValue { get; set; }
        public string Input { get; set; }
        public bool IsValid { get; set; }
        public DateTime CreationTime { get; set; }
        public string ErrorCode { get; set; }
    }
}