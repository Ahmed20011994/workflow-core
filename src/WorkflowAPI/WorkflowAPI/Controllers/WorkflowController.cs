using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using WorkflowAPI.Models;
using WorkflowCore.Interface;

namespace WorkflowAPI.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]/[action]")]
    public class WorkflowController : Controller
    {
        #region Public Constructors

        public WorkflowController(IWorkflowHost workflowHost, IWorkflowRegistry registry, IDefinitionLoader loader, IPersistenceProvider workflowStore, ILoggerFactory loggerFactory)
        {
            _workflowHost = workflowHost;
            _workflowStore = workflowStore;
            _registry = registry;
            _logger = loggerFactory.CreateLogger<WorkflowController>();
            _loader = loader;
        }

        #endregion Public Constructors

        #region Public Methods

        [HttpGet]
        [ActionName("Get")]
        public async Task<IActionResult> Get()
        {
            var result = await _workflowStore.GetWorkflowInstances(null, string.Empty, null, null, 0, 100);
            return Ok(result.ToList());
        }

        [HttpPost]
        [ActionName("StartWorkflow")]
        public async Task<IActionResult> StartWorkflow([FromBody]Workflow instance)
        {
            var result = await _workflowStore.FetchWorkFlows();
            var workFlow = result.Where(i => i.WorkflowName.ToLower() == instance.WorkflowName.ToLower());

            var dataObject = System.Reflection.Assembly.GetExecutingAssembly().CreateInstance(instance.DataClassName);

            _loader.LoadDefinition(JsonConvert.SerializeObject(workFlow.FirstOrDefault().Definition));

            string workflowId = await _workflowHost.StartWorkflow(instance.WorkflowName, 1, dataObject);
            return base.Ok(workflowId);
        }

        [HttpPost]
        [ActionName("PublishEvent")]
        public async Task<IActionResult> PublishEvent([FromBody]Event eventData)
        {
            var result = await _workflowStore.FetchWorkFlows();
            var workFlow = result.Where(i => i.WorkflowName.ToLower() == eventData.WorkflowName.ToLower());

            _loader.LoadDefinition(JsonConvert.SerializeObject(workFlow.FirstOrDefault().Definition));

            var dataObject = System.Reflection.Assembly.GetExecutingAssembly().CreateInstance(eventData.DataClassName);

            foreach(var item in eventData.Data)
            {
                dataObject.GetType().GetProperty(item.Key.ToString()).SetValue(dataObject, item.Value.ToString());
            }

            await _workflowHost.PublishEvent(eventData.EventName, eventData.InstanceId, dataObject);
            return Ok();
        }

        #endregion Public Methods

            #region Private Fields

        private readonly IWorkflowHost _workflowHost;
        private readonly IWorkflowRegistry _registry;
        private readonly IPersistenceProvider _workflowStore;
        private readonly IDefinitionLoader _loader;
        private readonly ILogger _logger;

        #endregion Private Fields
    }
}