using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WorkflowCore.Interface;

namespace WorkflowAPI.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class WorkflowController : Controller
    {
        #region Public Constructors

        public WorkflowController(IWorkflowHost workflowHost, IWorkflowRegistry registry, IPersistenceProvider workflowStore, ILoggerFactory loggerFactory)
        {
            _workflowHost = workflowHost;
            _workflowStore = workflowStore;
            _registry = registry;
            _logger = loggerFactory.CreateLogger<WorkflowController>();
        }

        #endregion Public Constructors

        #region Public Methods

        //[HttpGet]
        //public async Task<IActionResult> Get(WorkflowStatus? status, string type, DateTime? createdFrom, DateTime? createdTo, int skip, int take)
        //{
        //    var result = await _workflowStore.GetWorkflowInstances(status, type, createdFrom, createdTo, skip, take);
        //    return Json(result.ToList());
        //}

        [HttpGet(Name = "Get")]
        public async Task<IActionResult> Get()
        {
            //var result = await _workflowStore.GetWorkflowInstances(WorkflowStatus.Runnable, string.Empty, null, null, 0, 100);
            var result = await _workflowStore.GetWorkflowInstances(null, string.Empty, null, null, 0, 100);
            return Json(result.ToList());
        }

        #endregion Public Methods

        //// GET: api/Workflow
        //[HttpGet]
        //public IEnumerable<string> Get()
        //{
        //    return new string[] { "value1", "value2" };
        //}

        // GET: api/Workflow/5
        //[HttpGet("{id}", Name = "Get")]
        //public string Get(int id)
        //{
        //    return "value";
        //}
        
        // POST: api/Workflow
        [HttpPost]
        public void Post([FromBody]string value)
        {
        }
        
        // PUT: api/Workflow/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }
        
        // DELETE: api/ApiWithActions/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }

        #region Private Fields

        private readonly IWorkflowHost _workflowHost;
        private readonly IWorkflowRegistry _registry;
        private readonly IPersistenceProvider _workflowStore;
        private readonly ILogger _logger;

        #endregion Private Fields
    }
}