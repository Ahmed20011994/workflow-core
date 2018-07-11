using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using WorkflowAPI.Data;
using WorkflowAPI.Models;
using WorkflowCore.Interface;

namespace WorkflowAPI.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
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
        public async Task<IActionResult> Get()
        {
            var result = await _workflowStore.GetWorkflowInstances(null, string.Empty, null, null, 0, 100);
            return Ok(result.ToList());
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody]Workflow instance)
        {
            var result = await _workflowStore.FetchWorkFlows();
            var workFlow = result.Where(i => i.WorkflowName.ToLower() == instance.WorkflowName.ToLower());

            _loader.LoadDefinition(JsonConvert.SerializeObject(workFlow.FirstOrDefault().Definition));


            await _workflowHost.StartWorkflow(instance.WorkflowName, 1, new MyDataClass());

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