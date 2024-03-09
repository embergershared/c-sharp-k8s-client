using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace WorkerJob
{
    public class Job : IJob
    {
        private readonly ILogger<Job> _logger;

        public Job(ILogger<Job> logger)
        {
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            _logger.LogInformation("Job was executedAsync()");
            await Task.CompletedTask;
        }
    }
}