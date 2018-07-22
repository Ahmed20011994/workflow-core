using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using WorkflowAPI.Output;
using WorkflowCore.Interface;
using WorkflowAPI.Models;
using System.Collections.Generic;
using System.Reflection;

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
        public async Task<IActionResult> StartWorkflow([FromBody] Workflow instance)
        {
            var result = await _workflowStore.FetchWorkFlows();
            var workFlow = result.Where(i => i.WorkflowName.ToLower() == instance.WorkflowName.ToLower());
            var dataType = string.Empty;
            object dataObject = null;
            var pointer = new WorkflowCore.Models.ExecutionPointer();
            string eventName = string.Empty;
            var expectedInputs = new List<ExpectedInput>();
            var inputs = new List<string>();

            if (workFlow != null)
            {
                dataType = workFlow.FirstOrDefault().Definition.DataType.Split(",").FirstOrDefault();
            }

            if(dataType != null)
            {
                dataObject = System.Reflection.Assembly.GetExecutingAssembly().CreateInstance(dataType);
                _loader.LoadDefinition(JsonConvert.SerializeObject(workFlow.FirstOrDefault().Definition));
            }

            string workflowId = await _workflowHost.StartWorkflow(instance.WorkflowName, 1, dataObject);

            //var WorkflowInstance = await _workflowStore.GetWorkflowInstance(workflowId);

            var output = await GetWorkflowOutput(workflowId, workFlow.FirstOrDefault(), dataObject);

            //if(WorkflowInstance != null)
            //{
            //    workflowStatus = WorkflowInstance.Status;
            //    pointer = WorkflowInstance.ExecutionPointers.Last();

            //    while ( (pointer.Status == WorkflowCore.Models.PointerStatus.Pending || pointer.Status == WorkflowCore.Models.PointerStatus.Running) || (pointer.Status == WorkflowCore.Models.PointerStatus.Complete && workflowStatus == WorkflowCore.Models.WorkflowStatus.Running))
            //    {
            //        WorkflowInstance = await _workflowStore.GetWorkflowInstance(workflowId);
            //        workflowStatus = WorkflowInstance.Status;
            //        pointer = WorkflowInstance.ExecutionPointers.Last();
            //    }
            //}

            //if((pointer.Status == WorkflowCore.Models.PointerStatus.WaitingForEvent) && (!pointer.EventPublished))
            //{
            //    eventName = pointer.EventName;
            //    var step = workFlow.FirstOrDefault().Definition.Steps.Where(s => s.Name == pointer.StepName).FirstOrDefault();
            //    if (step.Outputs != null)
            //    {
            //        var outputs = step.Outputs;
            //        foreach(var item in outputs)
            //        {
            //            inputs.Add(item.Key);
            //        }
            //    }
            //}

            //foreach(var input in inputs)
            //{
            //    var type = dataObject.GetType().GetProperty(input);

            //    if(type != null)
            //    {

            //        var expectedInput = new ExpectedInput
            //        {
            //            Name = input,
            //            DataType = type.PropertyType.Name
            //        };

            //        expectedInputs.Add(expectedInput);
            //    }
            //}

            //WorkflowOutput output = new WorkflowOutput
            //{
            //    InstanceId = workflowId,
            //    WorkflowStatus = workflowStatus.ToString(),
            //    LastStepName = pointer.StepName,
            //    StepStatus = pointer.Status.ToString(),
            //    EventName = eventName,
            //    ExpectedInputs = expectedInputs
            //};

            return Ok(output);
        }

        [HttpPost]
        [ActionName("PublishEvent")]
        public async Task<IActionResult> PublishEvent([FromBody]Event eventData)
        {
            var pointer = new WorkflowCore.Models.ExecutionPointer();
            string eventName = string.Empty;
            var expectedInputs = new List<ExpectedInput>();
            var inputs = new List<string>();

            var WorkflowInstance = await _workflowStore.GetWorkflowInstance(eventData.InstanceId);

            var result = await _workflowStore.FetchWorkFlows();
            var workFlow = result.Where(i => i.Definition.Id == WorkflowInstance.WorkflowDefinitionId);

            var dataType = workFlow.FirstOrDefault().Definition.DataType.Split(",").FirstOrDefault();

            _loader.LoadDefinition(JsonConvert.SerializeObject(workFlow.FirstOrDefault().Definition));

            var dataObject = Assembly.GetExecutingAssembly().CreateInstance(dataType);

            foreach(var item in eventData.Data)
            {
                dataObject.GetType().GetProperty(item.Key.ToString()).SetValue(dataObject, item.Value.ToString());
            }

            await _workflowHost.PublishEvent(eventData.EventName, eventData.InstanceId, dataObject);

            var output = await GetWorkflowOutput(eventData.InstanceId, workFlow.FirstOrDefault(), dataObject, eventData.EventName);

            return Ok(output);
        }

        #endregion Public Methods

        #region Private Methods
        public async Task<WorkflowOutput> GetWorkflowOutput(string instanceId, WorkflowCore.Models.Workflow workFlow, object dataObject, string publishedEvent = null)
        {
            var pointer = new WorkflowCore.Models.ExecutionPointer();
            var workflowStatus = WorkflowCore.Models.WorkflowStatus.Running;
            string eventName = string.Empty;
            var expectedInputs = new List<ExpectedInput>();
            var inputs = new List<string>();

            var WorkflowInstance = await _workflowStore.GetWorkflowInstance(instanceId);

            if (WorkflowInstance != null)
            {
                workflowStatus = WorkflowInstance.Status;
                pointer = WorkflowInstance.ExecutionPointers.Last();

                while ((pointer.Status == WorkflowCore.Models.PointerStatus.Pending || pointer.Status == WorkflowCore.Models.PointerStatus.Running) || (pointer.Status == WorkflowCore.Models.PointerStatus.Complete && workflowStatus == WorkflowCore.Models.WorkflowStatus.Running) || (pointer.Status == WorkflowCore.Models.PointerStatus.WaitingForEvent && pointer.EventName == publishedEvent))
                {
                    WorkflowInstance = await _workflowStore.GetWorkflowInstance(instanceId);
                    workflowStatus = WorkflowInstance.Status;
                    pointer = WorkflowInstance.ExecutionPointers.Last();
                }
            }

            if ((pointer.Status == WorkflowCore.Models.PointerStatus.WaitingForEvent) && (!pointer.EventPublished))
            {
                eventName = pointer.EventName;
                var step = workFlow.Definition.Steps.Where(s => s.Name == pointer.StepName).FirstOrDefault();
                if (step.Outputs != null)
                {
                    var outputs = step.Outputs;
                    foreach (var item in outputs)
                    {
                        inputs.Add(item.Key);
                    }
                }
            }

            foreach (var input in inputs)
            {
                var type = dataObject.GetType().GetProperty(input);

                if (type != null)
                {

                    var expectedInput = new ExpectedInput
                    {
                        Name = input,
                        DataType = type.PropertyType.Name
                    };

                    expectedInputs.Add(expectedInput);
                }
            }

            WorkflowOutput output = new WorkflowOutput
            {
                InstanceId = instanceId,
                WorkflowStatus = workflowStatus.ToString(),
                LastStepName = pointer.StepName,
                StepStatus = pointer.Status.ToString(),
                EventName = eventName,
                ExpectedInputs = expectedInputs
            };

            return output;
        }

        #region Private Methods


        #endregion Private Fields

        private readonly IWorkflowHost _workflowHost;
        private readonly IWorkflowRegistry _registry;
        private readonly IPersistenceProvider _workflowStore;
        private readonly IDefinitionLoader _loader;
        private readonly ILogger _logger;

        #endregion Private Fields
    }
}