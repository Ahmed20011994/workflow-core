using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WorkflowCore;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace TestWorkflow
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var serviceProvider = ConfigureServices();

            //start the workflow host
            var host = serviceProvider.GetService<IWorkflowHost>();
            var controller = serviceProvider.GetService<IWorkflowController>();
            var loader = serviceProvider.GetService<IDefinitionLoader>();
            var str = Resources.HelloWorld;

            //var definition = controller.GetWorkflowFromRegistry("testworkflow").Result;

            //loader.LoadDefinition(JsonConvert.DeserializeObject(definition).ToString());

            loader.LoadDefinition(str);

            host.Start();

            host.StartWorkflow("HelloWorld", 1, new MyDataClass());

            Console.ReadLine();
            host.Stop();
        }

        private static IServiceProvider ConfigureServices()
        {
            //setup dependency injection
            IServiceCollection services = new ServiceCollection();
            services.AddLogging();
            //services.AddWorkflow();
            services.AddWorkflow(x =>
                x.UseSqlServer(
                    @"Server=tcp:workflowcore.database.windows.net,1433;Initial Catalog=WorkflowCore;Persist Security Info=False;User ID=ahmedali;Password=Optimist2094;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Database=WorkflowCore;Trusted_Connection=False;",
                    true, true));
            //services.AddTransient<GoodbyeWorld>();

            var serviceProvider = services.BuildServiceProvider();

            //config logging
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            loggerFactory.AddDebug();
            return serviceProvider;
        }
    }

    public class HelloWorld : StepBody
    {
        public override ExecutionResult Run(IStepExecutionContext context)
        {
            Console.WriteLine("Hello world");
            return ExecutionResult.Next();
        }
    }
    public class GoodbyeWorld : StepBody
    {
        public override ExecutionResult Run(IStepExecutionContext context)
        {
            Console.WriteLine("Goodbye world");
            return ExecutionResult.Next();
        }
    }

    public class Throw : StepBody
    {
        public override ExecutionResult Run(IStepExecutionContext context)
        {
            Console.WriteLine("throwing...");
            throw new Exception("up");
        }
    }

    public class PrintMessage : StepBody
    {
        public string Message { get; set; }

        public override ExecutionResult Run(IStepExecutionContext context)
        {
            Console.WriteLine(Message);
            return ExecutionResult.Next();
        }
    }

    public class GenerateMessage : StepBody
    {
        public string Message { get; set; }

        public override ExecutionResult Run(IStepExecutionContext context)
        {
            Message = "Generated message";
            return ExecutionResult.Next();
        }
    }

    //public class MyDataClass
    //{
    //    public int Value1 { get; set; }

    //    public int Value2 { get; set; }

    //    public int Value3 { get; set; }
    //}

    public class AddNumbers : StepBody
    {
        public int Input1 { get; set; }

        public int Input2 { get; set; }

        public int Output { get; set; }


        public override ExecutionResult Run(IStepExecutionContext context)
        {
            Output = (Input1 + Input2);
            return ExecutionResult.Next();
        }
    }
}