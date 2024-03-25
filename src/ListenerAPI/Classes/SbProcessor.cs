// Background tasks with hosted services in ASP.NET Core
// Ref: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-8.0&tabs=visual-studio
// 
// The task creates a processor on the ServiceBus queue(s) to listen to messages.
// Ref: https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-dotnet-get-started-with-queues?tabs=passwordless#add-the-code-to-receive-messages-from-the-queue

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Azure.Messaging.ServiceBus;
using IdentityModel.OidcClient;
using ListenerAPI.Helpers;
using ListenerAPI.Models;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ListenerAPI.Classes
{
  public class SbProcessor : IHostedService, IDisposable
  {
    private readonly ILogger<SbProcessor> _logger;
    private readonly IAzureClientFactory<ServiceBusClient> _sbClientFactory;
    private readonly IMapper _mapper;
    private readonly SbNsQueue _queue;

    private ServiceBusProcessor? _processor;

    public SbProcessor(
      ILogger<SbProcessor> logger,
      IConfiguration config,
      IAzureClientFactory<ServiceBusClient> sbClientFactory,
      IMapper mapper
    )
    {
      _logger = logger;
      _sbClientFactory = sbClientFactory ?? throw new ArgumentNullException(nameof(sbClientFactory));
      _mapper = mapper;
      _queue = AppGlobal.GetNames(config);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
      _logger.LogDebug("SbProcessor.StartAsync() called.");

      _logger.LogDebug(
        "Processor will plug on the queue: {queueName}, in ServiceBus: {sbName}.",
        _queue.QueueName,
        _queue.SbNamespace);

      _processor = _sbClientFactory.CreateClient(_queue.SbNamespace).CreateProcessor(_queue.QueueName);

      // add handler to process messages
      _processor.ProcessMessageAsync += MessageHandler;

      // add handler to process any errors
      _processor.ProcessErrorAsync += ErrorHandler;

      // start processing 
      await _processor.StartProcessingAsync(cancellationToken);


      _logger.LogDebug("SbProcessor.StartAsync() finished.");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
      _logger.LogDebug("SbProcessor.StopAsync() called.");
      return Task.CompletedTask;
    }

    public async void Dispose()
    {
      _logger.LogDebug("SbProcessor.Dispose() called.");
      if (_processor != null) await _processor.DisposeAsync();
      GC.SuppressFinalize(this);
    }

    #region Private Methods
    // handle received messages
    private async Task MessageHandler(ProcessMessageEventArgs args)
    {
      var receivedMessage = _mapper.Map<ReceivedMessage>(args.Message);
      receivedMessage.IsSuccessfullyReceived = true;
      receivedMessage.QueueName = args.EntityPath;
      receivedMessage.ServiceBusName = StringHelper.RemoveSbSuffix(args.FullyQualifiedNamespace);

      _logger.LogInformation("SbProcessor.MessageHandler(): Received the following message: {message}", JsonSerializer.Serialize(receivedMessage));

      // complete the message. message is deleted from the queue. 
      await args.CompleteMessageAsync(args.Message);
    }

    // handle any errors when receiving messages
    private Task ErrorHandler(ProcessErrorEventArgs args)
    {
      _logger.LogError("SbProcessor.ErrorHandler(): Message reception error {error}", args.Exception.ToString());
      return Task.CompletedTask;
    }
    
    #endregion
  }
}
