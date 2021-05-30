using Cronos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
using UNIT3D_Helper.Entities;
using UNIT3D_Helper.Helpers;
using UNIT3D_Helper.Services;

namespace UNIT3D_Helper
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceProvider _services;
        private readonly WorkerOptions _workerOptions;

        public Worker(IServiceProvider services,IOptions<WorkerOptions> workerOptions, ILogger<Worker> logger)
        {
            _logger = logger;
            _services = services;
            _workerOptions = workerOptions?.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _services.CreateScope();
                var unit3dClient = scope.ServiceProvider.GetRequiredService<Unit3dClient>();
                await unit3dClient.ExecuteAsync(_workerOptions.FilesInRowReadyPreviouslyBeforeStop,stoppingToken);

                CronExpression expression = CronExpression.Parse(_workerOptions.ExecutionCron);
                DateTime? next = expression.GetNextOccurrence(DateTime.UtcNow, TimeZoneInfo.Utc).Value;
                var delay = (next.Value - DateTime.UtcNow).TotalMilliseconds;
                _logger.LogInformation("Next Cycle at: {time}", (DateTimeOffset)next.Value);
                await Tools.SafeDelayAsync((int)delay, stoppingToken);
            }
        }
    }
}
