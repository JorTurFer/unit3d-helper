using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Prometheus;
using System;
using System.Collections.Generic;
using UNIT3D_Helper.Entities;
using UNIT3D_Helper.Services;

namespace UNIT3D_Helper
{
    public class Program
    {
        public static void Main(string[] args)
        {
            StartPrometheusServer();
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.Configure<TrackerOptions>(hostContext.Configuration.GetSection(TrackerOptions.SectionName));
                    services.Configure<WorkerOptions>(hostContext.Configuration.GetSection(WorkerOptions.SectionName));
                    services.AddHttpClient<Unit3dClient>((services,client) => 
                    {
                        var trackerOptions = services.GetService<IOptions<TrackerOptions>>();
                        client.BaseAddress = new Uri(trackerOptions.Value.Url);
                        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/90.0.4430.212 Safari/537.36 Edg/90.0.818.66");
                    });
                    
                    services.AddHostedService<Worker>();
                });

        public static void StartPrometheusServer()
        {
            Metrics.SuppressDefaultMetrics();
            var server = new MetricServer(hostname: "localhost", port: 9090);
            server.Start();
        }
    }
}
