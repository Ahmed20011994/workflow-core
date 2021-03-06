﻿using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace WorkflowCore.Services
{
    public class WorkflowExecutor : IWorkflowExecutor
    {
        protected readonly IWorkflowRegistry _registry;
        protected readonly IServiceProvider _serviceProvider;
        protected readonly IDateTimeProvider _datetimeProvider;
        protected readonly ILogger _logger;
        private readonly IExecutionResultProcessor _executionResultProcessor;
        private readonly WorkflowOptions _options;
        private readonly IPersistenceProvider _persistenceStore;

        private IWorkflowHost Host => _serviceProvider.GetService<IWorkflowHost>();

        public WorkflowExecutor(IWorkflowRegistry registry, IServiceProvider serviceProvider, IDateTimeProvider datetimeProvider, IExecutionResultProcessor executionResultProcessor, WorkflowOptions options, ILoggerFactory loggerFactory, IPersistenceProvider persistenceStore)
        {
            _serviceProvider = serviceProvider;
            _registry = registry;
            _datetimeProvider = datetimeProvider;
            _options = options;
            _logger = loggerFactory.CreateLogger<WorkflowExecutor>();
            _executionResultProcessor = executionResultProcessor;
            _persistenceStore = persistenceStore;
        }

        public async Task<WorkflowExecutorResult> Execute(WorkflowInstance workflow)

        {
            var wfResult = new WorkflowExecutorResult();
            wfResult.IsValidated = true;

            var exePointers = new List<ExecutionPointer>(workflow.ExecutionPointers.Where(x => x.Active && (!x.SleepUntil.HasValue || x.SleepUntil < _datetimeProvider.Now.ToUniversalTime())));
            var def = _registry.GetDefinition(workflow.WorkflowDefinitionId, workflow.Version);
            if (def == null)
            {
                _logger.LogError("Workflow {0} version {1} is not registered", workflow.WorkflowDefinitionId, workflow.Version);
                return wfResult;
            }

            foreach (var pointer in exePointers)
            {
                var step = def.Steps.First(x => x.Id == pointer.StepId);
                if (step != null)
                {
                    try
                    {
                        pointer.Status = PointerStatus.Running;
                        switch (step.InitForExecution(wfResult, def, workflow, pointer))
                        {
                            case ExecutionPipelineDirective.Defer:
                                continue;
                            case ExecutionPipelineDirective.EndWorkflow:
                                workflow.Status = WorkflowStatus.Complete;
                                workflow.CompleteTime = _datetimeProvider.Now.ToUniversalTime();
                                continue;
                        }

                        if (!pointer.StartTime.HasValue)
                        {
                            pointer.StartTime = _datetimeProvider.Now.ToUniversalTime();
                        }

                        _logger.LogDebug("Starting step {0} on workflow {1}", step.Name, workflow.Id);

                        IStepBody body = step.ConstructBody(_serviceProvider);

                        if (body == null)
                        {
                            _logger.LogError("Unable to construct step body {0}", step.BodyType.ToString());
                            pointer.SleepUntil = _datetimeProvider.Now.ToUniversalTime().Add(_options.ErrorRetryInterval);
                            wfResult.Errors.Add(new ExecutionError()
                            {
                                WorkflowId = workflow.Id,
                                ExecutionPointerId = pointer.Id,
                                ErrorTime = _datetimeProvider.Now.ToUniversalTime(),
                                Message = String.Format("Unable to construct step body {0}", step.BodyType.ToString())
                            });
                            continue;
                        }

                        IStepExecutionContext context = new StepExecutionContext()
                        {
                            Workflow = workflow,
                            Step = step,
                            PersistenceData = pointer.PersistenceData,
                            ExecutionPointer = pointer,
                            Item = pointer.ContextItem
                        };

                        ProcessInputs(workflow, step, body, context);

                        switch (step.BeforeExecute(wfResult, context, pointer, body))
                        {
                            case ExecutionPipelineDirective.Defer:
                                continue;
                            case ExecutionPipelineDirective.EndWorkflow:
                                workflow.Status = WorkflowStatus.Complete;
                                workflow.CompleteTime = _datetimeProvider.Now.ToUniversalTime();
                                continue;
                        }

                        var result = await body.RunAsync(context);

                        if (result.Proceed)
                        {
                            ProcessOutputs(workflow, step, body);
                        }

                        if (step.BodyType.Name == "WaitFor" && context.ExecutionPointer.EventPublished)
                        {
                            var eventData = context.ExecutionPointer.EventData;
                            var validation = await ProcessValidations(workflow, step, eventData, context.ExecutionPointer.Id);

                            if(!validation) //Validation Failed
                            {
                                pointer.Status = PointerStatus.ValidationFailed;
                                pointer.Active = false;
                                pointer.EndTime = DateTime.UtcNow;
                                workflow.Status = WorkflowStatus.ValidationFailed;
                                wfResult.IsValidated = false;
                            }
                        }

                        _executionResultProcessor.ProcessExecutionResult(workflow, def, pointer, step, result, wfResult);
                        step.AfterExecute(wfResult, context, result, pointer);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Workflow {0} raised error on step {1} Message: {2}", workflow.Id, pointer.StepId, ex.Message);
                        wfResult.Errors.Add(new ExecutionError()
                        {
                            WorkflowId = workflow.Id,
                            ExecutionPointerId = pointer.Id,
                            ErrorTime = _datetimeProvider.Now.ToUniversalTime(),
                            Message = ex.Message
                        });

                        _executionResultProcessor.HandleStepException(workflow, def, pointer, step);
                        Host.ReportStepError(workflow, step, ex);
                    }
                }
                else
                {
                    _logger.LogError("Unable to find step {0} in workflow definition", pointer.StepId);
                    pointer.SleepUntil = _datetimeProvider.Now.ToUniversalTime().Add(_options.ErrorRetryInterval);
                    wfResult.Errors.Add(new ExecutionError()
                    {
                        WorkflowId = workflow.Id,
                        ExecutionPointerId = pointer.Id,
                        ErrorTime = _datetimeProvider.Now.ToUniversalTime(),
                        Message = String.Format("Unable to find step {0} in workflow definition", pointer.StepId)
                    });
                }

            }
            ProcessAfterExecutionIteration(workflow, def, wfResult);
            DetermineNextExecutionTime(workflow);

            return wfResult;
        }

        private void ProcessInputs(WorkflowInstance workflow, WorkflowStep step, IStepBody body, IStepExecutionContext context)
        {
            //TODO: Move to own class
            foreach (var input in step.Inputs)
            {
                var member = (input.Target.Body as MemberExpression);
                object resolvedValue = null;

                switch (input.Source.Parameters.Count)
                {
                    case 1:
                        resolvedValue = input.Source.Compile().DynamicInvoke(workflow.Data);
                        break;
                    case 2:
                        resolvedValue = input.Source.Compile().DynamicInvoke(workflow.Data, context);
                        break;
                    default:
                        throw new ArgumentException();
                }

                step.BodyType.GetProperty(member.Member.Name).SetValue(body, resolvedValue);
            }
        }

        private void ProcessOutputs(WorkflowInstance workflow, WorkflowStep step, IStepBody body)
        {
            foreach (var output in step.Outputs)
            {
                var member = (output.Target.Body as MemberExpression);
                if(member != null) // to resolve a field or property
                {
                    var list = new List<String>()
                    {
                        "system.int64", "system.int32", "system.string", "system.boolean"
                    };
                    var resolvedValue = output.Source.Compile().DynamicInvoke(body);

                    Type resolvedValueType = resolvedValue.GetType();

                    var type = resolvedValueType.FullName.ToLower();

                    if (!list.Contains(type))
                    {
                        var data = workflow.Data;
                        var value = resolvedValue.GetType().GetProperty(member.Member.Name).GetValue(resolvedValue, null);
                        data.GetType().GetProperty(member.Member.Name).SetValue(data, value);
                    }

                    else if (list.Contains(type))
                    {
                        var data = workflow.Data;
                        data.GetType().GetProperty(member.Member.Name).SetValue(data, resolvedValue);
                    }
                }

                if (member == null) // to resolve an object
                {
                    var resolvedValue = output.Source.Compile().DynamicInvoke(body);
                    var data = workflow.Data;

                    Type dataType = data.GetType();

                    foreach (PropertyInfo propertyInfo in dataType.GetProperties())
                    {
                        var value = resolvedValue.GetType().GetProperty(propertyInfo.Name).GetValue(resolvedValue, null);
                        data.GetType().GetProperty(propertyInfo.Name).SetValue(data, value);
                    }
                }
            }
        }

        private async Task<bool> ProcessValidations(WorkflowInstance workflow, WorkflowStep step, object eventData, string stepId)
        {
            var Validations = step.Validations;
            bool isvalidated = true;
            if(Validations != null)
            {
                foreach (var validation in Validations)
                {
                    var FieldNames = validation.Fields;
                    var DataValidations = validation.DataValidations;
                    
                    if(DataValidations != null)
                    {
                        foreach (var datavalidation in DataValidations)
                        {
                            if(FieldNames != null)
                            {
                                foreach (var fieldname in FieldNames)
                                {
                                    var InputValidation = new InputValidation
                                    {
                                        FieldName = fieldname,
                                        ErrorCode = datavalidation.ErrorCode,
                                        Input = string.IsNullOrEmpty(datavalidation.Input) ? string.Empty : datavalidation.Input,
                                        ValidationName = datavalidation.Name,
                                        StepId = stepId
                                    };

                                    if(!string.IsNullOrEmpty(datavalidation.Name))
                                    {
                                        MethodInfo method = typeof(Validation.Validations).GetMethod(datavalidation.Name);
                                        dynamic result = method.Invoke(null, new object[] { InputValidation, eventData});

                                        if(result != null)
                                        {
                                            InputValidation.FieldValue = result.FieldValue;
                                            InputValidation.IsValid = result.IsValid;
                                        }

                                        if(!InputValidation.IsValid)
                                        {
                                            isvalidated = false;
                                        }

                                        await _persistenceStore.PersistValidation(InputValidation);
                                    }
                                }
                            }
                        }
                    }

                }

                return isvalidated;
            }

            return isvalidated;
        }

        private void ProcessAfterExecutionIteration(WorkflowInstance workflow, WorkflowDefinition workflowDef, WorkflowExecutorResult workflowResult)
        {
            var pointers = workflow.ExecutionPointers.Where(x => x.EndTime == null);

            foreach (var pointer in pointers)
            {
                var step = workflowDef.Steps.First(x => x.Id == pointer.StepId);
                step?.AfterWorkflowIteration(workflowResult, workflowDef, workflow, pointer);
            }
        }

        private void DetermineNextExecutionTime(WorkflowInstance workflow)
        {
            workflow.NextExecution = null;

            if (workflow.Status == WorkflowStatus.Complete)
                return;

            foreach (var pointer in workflow.ExecutionPointers.Where(x => x.Active && (x.Children ?? new List<string>()).Count == 0))
            {
                if (!pointer.SleepUntil.HasValue)
                {
                    workflow.NextExecution = 0;
                    return;
                }

                long pointerSleep = pointer.SleepUntil.Value.ToUniversalTime().Ticks;
                workflow.NextExecution = Math.Min(pointerSleep, workflow.NextExecution ?? pointerSleep);
            }

            if (workflow.NextExecution == null)
            {
                foreach (var pointer in workflow.ExecutionPointers.Where(x => x.Active && (x.Children ?? new List<string>()).Count > 0))
                {
                    if (workflow.ExecutionPointers.Where(x => pointer.Children.Contains(x.Id)).All(x => x.EndTime.HasValue))
                    {
                        if (!pointer.SleepUntil.HasValue)
                        {
                            workflow.NextExecution = 0;
                            return;
                        }

                        long pointerSleep = pointer.SleepUntil.Value.ToUniversalTime().Ticks;
                        workflow.NextExecution = Math.Min(pointerSleep, workflow.NextExecution ?? pointerSleep);
                    }
                }
            }

            if ((workflow.NextExecution == null) && (workflow.ExecutionPointers.All(x => x.EndTime != null)))
            {
                workflow.Status = WorkflowStatus.Complete;
                workflow.CompleteTime = _datetimeProvider.Now.ToUniversalTime();
            }
        }
        
    }
}