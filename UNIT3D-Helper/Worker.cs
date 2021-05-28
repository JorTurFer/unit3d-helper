using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UNIT3D_Helper.Services;

namespace UNIT3D_Helper
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceProvider _services;

        public Worker(IServiceProvider services, ILogger<Worker> logger)
        {
            _logger = logger;
            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _services.CreateScope();
                var unit3dClient = scope.ServiceProvider.GetRequiredService<Unit3dClient>();
                await unit3dClient.ExecuteAsync(stoppingToken);
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
