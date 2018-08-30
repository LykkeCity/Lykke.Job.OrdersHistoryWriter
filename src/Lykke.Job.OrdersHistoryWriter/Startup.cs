using System;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common;
using Lykke.Common.ApiLibrary.Middleware;
using Lykke.Common.ApiLibrary.Swagger;
using Lykke.Common.Api.Contract.Responses;
using Lykke.Common.Log;
using Lykke.Job.OrdersHistoryWriter.Core.Services;
using Lykke.Job.OrdersHistoryWriter.Settings;
using Lykke.Job.OrdersHistoryWriter.Modules;
using Lykke.Logs;
using Lykke.Logs.Loggers.LykkeSlack;
using Lykke.SettingsReader;
using Lykke.MonitoringServiceApiCaller;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Converters;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Lykke.Job.OrdersHistoryWriter
{
    [PublicAPI]
    public class Startup
    {
        private const string ApiVersion = "v1";
        private const string ApiName = "OrdersHistoryWriter API";

        private string _monitoringServiceUrl;
        private ILog _log;
        private IHealthNotifier _healthNotifier;

        public IHostingEnvironment Environment { get; }
        public IContainer ApplicationContainer { get; private set; }
        public IConfigurationRoot Configuration { get; }

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
            Environment = env;
        }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            try
            {
                services.AddMvc()
                    .AddJsonOptions(options =>
                    {
                        options.SerializerSettings.Converters.Add(new StringEnumConverter());
                        options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver();
                    });

                services.AddSwaggerGen(options =>
                {
                    options.DefaultLykkeConfiguration(ApiVersion, ApiName);
                });

                var settingsManager = Configuration.LoadSettings<AppSettings>(options =>
                {
                    options.SetConnString(x => x.SlackNotifications.AzureQueue.ConnectionString);
                    options.SetQueueName(x => x.SlackNotifications.AzureQueue.QueueName);
                    options.SenderName = $"{AppEnvironment.Name} {AppEnvironment.Version}";
                });

                var appSettings = settingsManager.CurrentValue;

                if (appSettings.MonitoringServiceClient != null)
                    _monitoringServiceUrl = appSettings.MonitoringServiceClient.MonitoringServiceUrl;

                services.AddLykkeLogging(
                    settingsManager.ConnectionString(s => s.OrdersHistoryWriterJob.Db.LogsConnString),
                    "OrdersHistoryWriterLog",
                    appSettings.SlackNotifications.AzureQueue.ConnectionString,
                    appSettings.SlackNotifications.AzureQueue.QueueName,
                    logging =>
                    {
                        // This is necessary, if your service uses additional personal slack channel:
                        logging.AddAdditionalSlackChannel("LykkeReports", options =>
                        {
                            // Optional: default is LogLevel.Information
                            options.MinLogLevel = LogLevel.Warning;
                            // Optional: by default is enabled
                            options.SpamGuard.DisableGuarding();
                        });
                    });

                var builder = new ContainerBuilder();
                builder.Populate(services);

                builder.RegisterModule(new JobModule(appSettings.OrdersHistoryWriterJob));

                ApplicationContainer = builder.Build();

                var logFactory = ApplicationContainer.Resolve<ILogFactory>();
                _log = logFactory.CreateLog(this);
                _healthNotifier = ApplicationContainer.Resolve<IHealthNotifier>();

                return new AutofacServiceProvider(ApplicationContainer);
            }
            catch (Exception ex)
            {
                if (_log == null)
                    Console.WriteLine(ex);
                else
                    _log.Critical(ex);
                throw;
            }
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime appLifetime)
        {
            try
            {
                if (env.IsDevelopment())
                    app.UseDeveloperExceptionPage();

                app.UseLykkeForwardedHeaders();
                app.UseLykkeMiddleware(ex => new ErrorResponse {ErrorMessage = "Technical problem"});

                app.UseMvc();
                app.UseSwagger(c =>
                {
                    c.PreSerializeFilters.Add((swagger, httpReq) => swagger.Host = httpReq.Host.Value);
                });
                app.UseSwaggerUI(x =>
                {
                    x.RoutePrefix = "swagger/ui";
                    x.SwaggerEndpoint($"/swagger/{ApiVersion}/swagger.json", ApiVersion);
                });
                app.UseStaticFiles();

                appLifetime.ApplicationStarted.Register(() => StartApplication().GetAwaiter().GetResult());
                appLifetime.ApplicationStopping.Register(() => StopApplication().GetAwaiter().GetResult());
                appLifetime.ApplicationStopped.Register(CleanUp);
            }
            catch (Exception ex)
            {
                _log?.Critical(ex);
                throw;
            }
        }

        private async Task StartApplication()
        {
            try
            {
                // NOTE: Job not yet recieve and process IsAlive requests here

                await ApplicationContainer.Resolve<IStartupManager>().StartAsync();
                _healthNotifier.Notify("Started", Program.EnvInfo);

#if !DEBUG
                await Configuration.RegisterInMonitoringServiceAsync(_monitoringServiceUrl, _healthNotifier);
#endif
            }
            catch (Exception ex)
            {
                _log.Critical(ex);
                throw;
            }
        }

        private async Task StopApplication()
        {
            try
            {
                // NOTE: Job still can recieve and process IsAlive requests here, so take care about it if you add logic here.

                await ApplicationContainer.Resolve<IShutdownManager>().StopAsync();
            }
            catch (Exception ex)
            {
                _log?.Critical(ex);
                throw;
            }
        }

        private void CleanUp()
        {
            try
            {
                // NOTE: Job can't recieve and process IsAlive requests here, so you can destroy all resources
                _healthNotifier?.Notify("Terminating", Program.EnvInfo);

                ApplicationContainer.Dispose();
            }
            catch (Exception ex)
            {
                _log?.Critical(ex);
                throw;
            }
        }
    }
}
