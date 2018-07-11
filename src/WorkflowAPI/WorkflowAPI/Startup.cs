using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WorkflowAPI.Helpers.Extensions;
using WorkflowCore;
using WorkflowCore.Interface;

namespace WorkflowAPI
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public static IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddLogging();

            //services.AddWorkflow();

            services.AddWorkflow(x =>
                x.UseSqlServer(
                    @"Server=tcp:workflowcore.database.windows.net,1433;Initial Catalog=WorkflowCore;Persist Security Info=False;User ID=ahmedali;Password=Optimist2094;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Database=WorkflowCore;Trusted_Connection=False;",
                    true, true));

            services.AddCors();

            services.AddMvc();

            services.AddApiVersioning();

            services.AddSwaggerDocumentation();

            var serviceProvider = services.BuildServiceProvider();

            //config logging
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            loggerFactory.AddDebug();
            return serviceProvider;
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddFile("Workflows/SampleWorkflow.txt");

            var host = app.ApplicationServices.GetService<IWorkflowHost>();

            host.Start();

            app.ApplicationServices.GetService<IWorkflowController>();

            app.ApplicationServices.GetService<IDefinitionLoader>(); 

            app.UseCors(
                options => options.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()
            );

            if (env.IsDevelopment()) app.UseDeveloperExceptionPage();

            app.UseMvcWithDefaultRoute();

            app.UseSwaggerDocumentation();
        }
    }
}