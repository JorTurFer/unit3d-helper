using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using UNIT3D_Helper.Entities;
using UNIT3D_Helper.Services;

namespace UNIT3D_Helper
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.Configure<TrackerOptions>(hostContext.Configuration.GetSection(TrackerOptions.SectionName));
                    services.AddHttpClient<Unit3dClient>((services,client) => 
                    {
                        var trackerOptions = services.GetService<IOptions<TrackerOptions>>();
                        client.BaseAddress = trackerOptions.Value.Url;
                        client.DefaultRequestHeaders.Add("Cookie", trackerOptions.Value.Coockie);
                        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/90.0.4430.212 Safari/537.36 Edg/90.0.818.66'");
                    });
                    
                    services.AddHostedService<Worker>();
                });
    }
}
