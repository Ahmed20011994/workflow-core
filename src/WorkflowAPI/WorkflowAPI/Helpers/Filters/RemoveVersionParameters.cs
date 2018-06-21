using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;

namespace WorkflowAPI.Helpers.Filters
{
    public class RemoveVersionParameters : IOperationFilter
    {
        #region Public Methods

        public void Apply(Operation operation, OperationFilterContext context)
        {
            var versionParameter = operation.Parameters.Single(p => p.Name == "version");
            operation.Parameters.Remove(versionParameter);
        }

        #endregion Public Methods
    }
}