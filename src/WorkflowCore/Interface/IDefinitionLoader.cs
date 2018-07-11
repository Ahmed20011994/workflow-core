using WorkflowCore.Models;
using WorkflowCore.Models.DefinitionStorage.v1;

namespace WorkflowCore.Interface
{
    public interface IDefinitionLoader
    {
        WorkflowDefinition LoadDefinition(string json);

        WorkflowDefinition Convert(DefinitionSourceV1 source);
    }
}