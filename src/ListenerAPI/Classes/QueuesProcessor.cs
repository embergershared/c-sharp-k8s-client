// Background tasks with hosted services in ASP.NET Core
// Ref: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-8.0&tabs=visual-studio

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ListenerAPI.Classes
{
  public class QueuesProcessor : IHostedService, IDisposable
  {
    private readonly ILogger<QueuesProcessor> _logger;

    public QueuesProcessor(
      ILogger<QueuesProcessor> logger
    )
    {
      _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
      _logger.LogDebug("QueuesProcessor.StartAsync() called.");

      _logger.LogInformation("QueuesProcessor did something.");

      return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
      _logger.LogDebug("QueuesProcessor.StopAsync() called.");
      return Task.CompletedTask;
    }

    public void Dispose()
    {
      _logger.LogDebug("QueuesProcessor.Dispose() called.");

      GC.SuppressFinalize(this);
    }
  }
}
