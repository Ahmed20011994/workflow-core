﻿using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Persistence.EntityFramework.Models;
using WorkflowCore.Models;
using WorkflowCore.Persistence.EntityFramework.Interfaces;
namespace WorkflowCore.Persistence.EntityFramework.Services
{
    public class EntityFrameworkPersistenceProvider : IPersistenceProvider
    {
        private readonly bool _canCreateDB;
        private readonly bool _canMigrateDB;
        private readonly IWorkflowDbContextFactory _contextFactory;
        private readonly IWorkflowRegistry _registry;

        public EntityFrameworkPersistenceProvider(IWorkflowDbContextFactory contextFactory, bool canCreateDB, bool canMigrateDB)
        {
            _contextFactory = contextFactory;
            _canCreateDB = canCreateDB;
            _canMigrateDB = canMigrateDB;
        }

        public async Task<string> CreateEventSubscription(EventSubscription subscription)
        {
            using (var db = ConstructDbContext())
            {
                subscription.Id = Guid.NewGuid().ToString();
                var persistable = subscription.ToPersistable();
                var result = db.Set<PersistedSubscription>().Add(persistable);
                await db.SaveChangesAsync();
                return subscription.Id;
            }
        }

        public async Task<string> CreateNewWorkflow(WorkflowInstance workflow)
        {
            using (var db = ConstructDbContext())
            {
                workflow.Id = Guid.NewGuid().ToString();
                var persistable = workflow.ToPersistable();
                var result = db.Set<PersistedWorkflow>().Add(persistable);
                await db.SaveChangesAsync();
                return workflow.Id;
            }
        }

        public async Task<Workflow> RegisterWorkflow(Workflow workflow)
        {
            using (var db = ConstructDbContext())
            {
                var persistable = workflow.ToPersistable();
                var result = db.Set<PersistedRegistry>().Add(persistable);
                await db.SaveChangesAsync();
                return result.Entity.ToWorkflow();
            }
        }

        public async Task<InputValidation> PersistValidation(InputValidation validation)
        {
            using (var db = ConstructDbContext())
            {
                var persistable = validation.ToPersistable();
                var result = db.Set<PersistedValidation>().Add(persistable);
                await db.SaveChangesAsync();
                return result.Entity.ToValidation();
            }
        }

        public async Task<string> FetchFlowDefinition(string workflowName)
        {
            using (var db = ConstructDbContext())
            {
                var workFlow = await db.Set<PersistedRegistry>()
                    .Where(p => p.WorkflowName.ToLower() == workflowName.ToLower())
                    .FirstOrDefaultAsync();

                return workFlow != null ? workFlow.Definition : string.Empty; 
            }
        }

        public async Task<List<Workflow>> FetchWorkFlows()
        {
            var flows = new List<PersistedRegistry>();

            using (var db = ConstructDbContext())
            {
                flows = await db.Set<PersistedRegistry>()
                    .ToListAsync();
            }

            List<Workflow> result = new List<Workflow>();

            foreach (var flow in flows)
            {
                result.Add(flow.ToWorkflow());
            }

            return result;
        }

        public async Task<IEnumerable<string>> GetRunnableInstances(DateTime asAt)
        {
            using (var db = ConstructDbContext())
            {
                var now = asAt.ToUniversalTime().Ticks;
                var raw = await db.Set<PersistedWorkflow>()
                    .Where(x => x.NextExecution.HasValue && (x.NextExecution <= now) && (x.Status == WorkflowStatus.Running))
                    .Select(x => x.InstanceId)
                    .ToListAsync();

                return raw.Select(s => s.ToString()).ToList();
            }
        }

        public async Task<IEnumerable<WorkflowInstance>> GetWorkflowInstances(WorkflowStatus? status, string type, DateTime? createdFrom, DateTime? createdTo, int skip, int take)
        {
            using (var db = ConstructDbContext())
            {
                IQueryable<PersistedWorkflow> query = db.Set<PersistedWorkflow>()
                    .Include(wf => wf.ExecutionPointers)
                    .ThenInclude(ep => ep.ExtensionAttributes)
                    .Include(wf => wf.ExecutionPointers)
                    .AsQueryable();

                if (status.HasValue)
                    query = query.Where(x => x.Status == status.Value);

                if (!String.IsNullOrEmpty(type))
                    query = query.Where(x => x.WorkflowDefinitionId == type);

                if (createdFrom.HasValue)
                    query = query.Where(x => x.CreateTime >= createdFrom.Value);

                if (createdTo.HasValue)
                    query = query.Where(x => x.CreateTime <= createdTo.Value);

                var rawResult = await query.Skip(skip).Take(take).ToListAsync();
                List<WorkflowInstance> result = new List<WorkflowInstance>();

                foreach (var item in rawResult)
                    result.Add(item.ToWorkflowInstance());

                return result;
            }
        }

        public async Task<WorkflowInstance> GetWorkflowInstance(string Id)
        {
            using (var db = ConstructDbContext())
            {
                var uid = new Guid(Id);
                var raw = await db.Set<PersistedWorkflow>()
                    .Include(wf => wf.ExecutionPointers)
                    .ThenInclude(ep => ep.ExtensionAttributes)
                    .Include(wf => wf.ExecutionPointers)
                    .FirstAsync(x => x.InstanceId == uid);

                if (raw == null)
                    return null;

                return raw.ToWorkflowInstance();
            }
        }

        public async Task PersistWorkflow(WorkflowInstance workflow)
        {
            using (var db = ConstructDbContext())
            {
                var uid = new Guid(workflow.Id);
                var existingEntity = await db.Set<PersistedWorkflow>()
                    .Where(x => x.InstanceId == uid)
                    .Include(wf => wf.ExecutionPointers)
                    .ThenInclude(ep => ep.ExtensionAttributes)
                    .Include(wf => wf.ExecutionPointers)
                    .AsTracking()
                    .FirstAsync();

                var persistable = workflow.ToPersistable(existingEntity);
                await db.SaveChangesAsync();
            }
        }

        public async Task TerminateSubscription(string eventSubscriptionId)
        {
            using (var db = ConstructDbContext())
            {
                var uid = new Guid(eventSubscriptionId);
                var existing = await db.Set<PersistedSubscription>().FirstAsync(x => x.SubscriptionId == uid);
                db.Set<PersistedSubscription>().Remove(existing);
                await db.SaveChangesAsync();
            }
        }

        public virtual void EnsureStoreExists()
        {
            using (var context = ConstructDbContext())
            {
                //if (_canCreateDB && !_canMigrateDB)
                //{
                //    context.Database.EnsureCreated();
                //    return;
                //}

                //if (_canMigrateDB)
                //{
                //    context.Database.Migrate();
                //    return;
                //}

                if (_canCreateDB)
                {
                    context.Database.EnsureCreated();
                    return;
                }
            }
        }

        public async Task<IEnumerable<EventSubscription>> GetSubcriptions(string eventName, string eventKey, DateTime asOf)
        {
            using (var db = ConstructDbContext())
            {
                asOf = asOf.ToUniversalTime();
                var raw = await db.Set<PersistedSubscription>()
                    .Where(x => x.EventName == eventName && x.EventKey == eventKey && x.SubscribeAsOf <= asOf)
                    .ToListAsync();

                return raw.Select(item => item.ToEventSubscription()).ToList();
            }
        }

        public async Task<string> CreateEvent(Event newEvent)
        {
            using (var db = ConstructDbContext())
            {
                newEvent.Id = Guid.NewGuid().ToString();
                var persistable = newEvent.ToPersistable();
                var result = db.Set<PersistedEvent>().Add(persistable);
                await db.SaveChangesAsync();
                return newEvent.Id;
            }
        }

        public async Task<Event> GetEvent(string id)
        {
            using (var db = ConstructDbContext())
            {
                Guid uid = new Guid(id);
                var raw = await db.Set<PersistedEvent>()
                    .FirstAsync(x => x.EventId == uid);

                if (raw == null)
                    return null;

                return raw.ToEvent();
            }
        }

        public async Task<IEnumerable<string>> GetRunnableEvents(DateTime asAt)
        {
            var now = asAt.ToUniversalTime();
            using (var db = ConstructDbContext())
            {
                asAt = asAt.ToUniversalTime();
                var raw = await db.Set<PersistedEvent>()
                    .Where(x => !x.IsProcessed)
                    .Where(x => x.EventTime <= now)
                    .Select(x => x.EventId)
                    .ToListAsync();

                return raw.Select(s => s.ToString()).ToList();
            }
        }

        public async Task MarkEventProcessed(string id)
        {
            using (var db = ConstructDbContext())
            {
                var uid = new Guid(id);
                var existingEntity = await db.Set<PersistedEvent>()
                    .Where(x => x.EventId == uid)
                    .AsTracking()
                    .FirstAsync();

                existingEntity.IsProcessed = true;
                await db.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<string>> GetEvents(string eventName, string eventKey, DateTime asOf)
        {
            using (var db = ConstructDbContext())
            {
                var raw = await db.Set<PersistedEvent>()
                    .Where(x => x.EventName == eventName && x.EventKey == eventKey)
                    .Where(x => x.EventTime >= asOf)
                    .Select(x => x.EventId)
                    .ToListAsync();

                var result = new List<string>();

                foreach (var s in raw)
                    result.Add(s.ToString());

                return result;
            }
        }

        public async Task MarkEventUnprocessed(string id)
        {
            using (var db = ConstructDbContext())
            {
                var uid = new Guid(id);
                var existingEntity = await db.Set<PersistedEvent>()
                    .Where(x => x.EventId == uid)
                    .AsTracking()
                    .FirstAsync();

                existingEntity.IsProcessed = false;
                await db.SaveChangesAsync();
            }
        }

        public async Task PersistErrors(IEnumerable<ExecutionError> errors)
        {
            using (var db = ConstructDbContext())
            {
                var executionErrors = errors as ExecutionError[] ?? errors.ToArray();
                if (executionErrors.Any())
                {
                    foreach (var error in executionErrors)
                    {
                        db.Set<PersistedExecutionError>().Add(error.ToPersistable());
                    }
                    await db.SaveChangesAsync();

                }
            }
        }

        private WorkflowDbContext ConstructDbContext()
        {
            return _contextFactory.Build();
        }
    }
}