using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Job
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;

        public Worker(
            ILogger<Worker> logger,
            IConfiguration configuration,
            IHostApplicationLifetime hostApplicationLifetime)
        {
            _logger = logger;
            _configuration = configuration;
            _hostApplicationLifetime = hostApplicationLifetime;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogTrace("Starting async Task ExecuteAsync() at: {time}", DateTimeOffset.Now);

            await DoIterationsAsync(stoppingToken);

            _logger.LogTrace("Ended async Task ExecuteAsync() at: {time}", DateTimeOffset.Now);
            _hostApplicationLifetime.StopApplication();
        }

        private async Task DoIterationsAsync(CancellationToken stoppingToken)
        {
            _logger.LogTrace("Starting async Task DoIterationsAsync() at: {time}", DateTimeOffset.Now);

            var iterations = _configuration.GetValue<int>("ITERATIONS");
            _logger.LogDebug("Starting Worker run for {iterations} iterations at: {time}", iterations, DateTimeOffset.Now);

            var i = 0;
            while (!stoppingToken.IsCancellationRequested)
            {
                if (i++ >= iterations)
                {
                    _logger.LogInformation("Worker completed {iterations} iterations at: {time}", iterations,
                        DateTimeOffset.Now);
                    break;
                }

                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Worker running iteration {iteration}/{iterations} at: {time}", i, iterations, DateTimeOffset.Now);
                }

                await Task.Delay(1000, stoppingToken);
            }

            _logger.LogTrace("Ended async Task DoIterationsAsync() at: {time}", DateTimeOffset.Now);
        }
    }
}
