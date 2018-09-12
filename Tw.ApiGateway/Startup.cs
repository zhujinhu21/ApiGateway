using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Consul;
using Swashbuckle.AspNetCore.Swagger;
using System.Collections.Generic;
using System.Linq;
using System;
using App.Metrics;

namespace ApiGateway
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOcelot().AddConsul();
            services.AddMvcCore(option =>
            {
            }).AddAuthorization().AddJsonFormatters();

            services.AddAuthentication("Bearer")
                .AddIdentityServerAuthentication(options =>
                {
                    options.Authority = "http://localhost:5001";
                    options.RequireHttpsMetadata = false;
                    options.ApiName = "TWAPI";
                });

            RegisterAppMetrics(services);
            services.AddMvc();
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("ApiGateway", new Info { Title = "网关服务", Version = "v1" });
                
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            var apis = new List<string> { "BookingApi", "PassengerApi" };
            app.UseMvc()
               .UseSwagger()
               .UseSwaggerUI(options =>
               {
                   apis.ForEach(m =>
                   {
                       options.SwaggerEndpoint($"/{m}/swagger.json", m);
                   });
               });

            bool isOpenMetrics = Convert.ToBoolean(Configuration["AppMetrics:IsOpen"]);
            if (isOpenMetrics)
            {
                app.UseMetricsAllEndpoints();
                app.UseMetricsAllMiddleware();
            }

            app.UseAuthentication();
            
            app.UseOcelot().Wait();
        }

        public void RegisterAppMetrics(IServiceCollection services)
        {
            bool isOpenMetrics = Convert.ToBoolean(Configuration["AppMetrics:IsOpen"]);
            if (isOpenMetrics)
            {
                string database = Configuration["AppMetrics:DatabaseName"];
                string connStr = Configuration["AppMetrics:ConnectionString"];
                string app = Configuration["AppMetrics:App"];
                string env = Configuration["AppMetrics:Env"];
                string username = Configuration["AppMetrics:UserName"];
                string password = Configuration["AppMetrics:Password"];

                var uri = new Uri(connStr);
                var metrics = AppMetrics.CreateDefaultBuilder().Configuration.Configure(options =>
                {
                    options.AddAppTag(app);
                    options.AddEnvTag(env);
                }).Report.ToInfluxDb(options =>
                {
                    options.InfluxDb.BaseUri = uri;
                    options.InfluxDb.Database = database;
                    options.InfluxDb.UserName = username;
                    options.InfluxDb.Password = password;
                    options.HttpPolicy.BackoffPeriod = TimeSpan.FromSeconds(30);
                    options.HttpPolicy.FailuresBeforeBackoff = 5;
                    options.HttpPolicy.Timeout = TimeSpan.FromSeconds(10);
                    options.FlushInterval = TimeSpan.FromSeconds(5);
                }).Build();

                services.AddMetrics(metrics);
                services.AddMetricsReportScheduler();
                services.AddMetricsTrackingMiddleware();
                services.AddMetricsEndpoints();
            }
        }
    }
}
