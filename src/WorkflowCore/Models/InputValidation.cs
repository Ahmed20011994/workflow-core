namespace WorkflowCore.Models
{
    public class InputValidation
    {
        public string FieldName { get; set; }
        public string FieldValue { get; set; }
        public string ValidationName { get; set; }
        public string Input { get; set; }
        public string ErrorCode { get; set; }
        public bool IsValid { get; set; }
        public string StepId { get; set; }
    }
}