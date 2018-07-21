using System.Reflection;
using System.Text.RegularExpressions;
using WorkflowCore.Models;

namespace WorkflowCore.Validation
{
    public static class Validations
    {
        public static InputValidation Required(InputValidation validation, object eventData)
        {
            var property = eventData.GetType().GetProperty(validation.FieldName);

            if(property != null)
            {
                var value = property.GetValue(eventData, null).ToString();

                if (value != null)
                {
                    validation.FieldValue = value;
                    validation.IsValid = true;
                }
            }

            return validation;
        }

        public static InputValidation RegExp(InputValidation validation, object eventData)
        {
            var property = eventData.GetType().GetProperty(validation.FieldName);
            var regex = validation.Input;

            if (property != null)
            {
                var value = property.GetValue(eventData, null).ToString();

                if(value != null && regex != null)
                {
                    validation.FieldValue = value;
                    Regex rgx = new Regex(regex);

                    if (rgx.IsMatch(value))
                    {
                        validation.IsValid = true;
                    }
                }
            }

            return validation;
        }
    }
}