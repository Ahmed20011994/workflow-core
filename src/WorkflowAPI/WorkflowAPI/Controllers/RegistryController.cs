using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using WorkflowCore.Models.DefinitionStorage.v1;

namespace WorkflowAPI.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class RegistryController : Controller
    {
        #region Public Constructors

        public RegistryController(IWorkflowHost workflowHost, IWorkflowRegistry registry, IWorkflowController controller, IDefinitionLoader loader, IPersistenceProvider workflowStore, ILoggerFactory loggerFactory)
        {
            _workflowHost = workflowHost;
            _workflowStore = workflowStore;
            _registry = registry;
            _logger = loggerFactory.CreateLogger<WorkflowController>();
            _controller = controller;
            _loader = loader;
        }

        #endregion Public Constructors

        #region Public Methods

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var result = await _workflowStore.FetchWorkFlows();

            return Ok(result.ToList());
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromHeader]string workflowName, [FromHeader]string description, [FromBody]JObject definition)
        {
            var wf = new Workflow
            {
                WorkflowName = workflowName,
                Description = description,
                Definition = JsonConvert.DeserializeObject<DefinitionSourceV1>(definition.ToString()),
                CreationTime = DateTime.Now
            };

            var workflow = await _workflowStore.RegisterWorkflow(wf);

            return CreatedAtRoute(new { id = workflow.WorkflowId }, workflow);
        }

        #endregion Public Methods      

        #region Private Fields

        private readonly IWorkflowHost _workflowHost;
        private readonly IWorkflowRegistry _registry;
        private readonly IWorkflowController _controller;
        private readonly IDefinitionLoader _loader; 
        private readonly IPersistenceProvider _workflowStore;
        private readonly ILogger _logger;

        #endregion Private Fields
    }
}