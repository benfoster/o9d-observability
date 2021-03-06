using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using O9d.Metrics.AspNet;
using O9d.Observability;
using Prometheus;

namespace AspNetExample
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            Prometheus.Metrics.DefaultRegistry.SetStaticLabels(new Dictionary<string, string>
            {
                { "app", "aspnet-example" },
                { "env", "prod" }
            });

            services.AddObservability(builder =>
                builder.AddAspNetMetrics(options =>
                    options.ConfigureRequestDurationHistogram = histogram =>
                    {
                        histogram.Buckets = new[] { 0.01, 0.02, 0.05, 0.1, 0.2, 0.5, 0.75, 1, 2 };
                    }
                )
            );

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "AspNetExample", Version = "v1" });
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "AspNetExample v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapMetrics();
                endpoints.MapControllers();
            });
        }
    }
}
