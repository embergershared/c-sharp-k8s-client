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
using ListenerAPI.Constants;
using ListenerAPI.Helpers;
using ListenerAPI.Interfaces;
using ListenerAPI.Models;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace ListenerAPI.Classes
{
  public class SbProcessor : IHostedService, IDisposable
  {
    private readonly ILogger<SbProcessor> _logger;
    private readonly IAzureClientFactory<ServiceBusClient> _sbClientFactory;
    private readonly IMapper _mapper;
    private readonly SbNsQueue _queue;
    private ServiceBusProcessor? _processor;
    private readonly IK8SClient _k8SClient;
    private readonly IConfiguration _config;

    public SbProcessor(
      ILogger<SbProcessor> logger,
      IConfiguration config,
      IAzureClientFactory<ServiceBusClient> sbClientFactory,
      IMapper mapper,
      IK8SClient k8SClient
    )
    {
      _logger = logger;
      _sbClientFactory = sbClientFactory ?? throw new ArgumentNullException(nameof(sbClientFactory));
      _mapper = mapper;
      _queue = new SbNsQueue(config, ConfigKey.SbNsQueueName);
      _k8SClient = k8SClient;
      _config = config;
    }

    #region Interface implementation
    public async Task StartAsync(CancellationToken cancellationToken)
    {
      _logger.LogDebug("SbProcessor.StartAsync() called.");

      _logger.LogDebug(
        "Processor will plug on the queue: {queueName}, in ServiceBus: {sbName}.",
        _queue.QueueName,
        _queue.SbNamespace);

      _processor = _sbClientFactory.CreateClient(_queue.SbNamespace).CreateProcessor(_queue.QueueName, GetServiceBusProcessorOptions);

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
    #endregion

    #region Private Methods
    // handle received messages
    private async Task MessageHandler(ProcessMessageEventArgs args)
    {
      var receivedMessage = _mapper.Map<ReceivedMessage>(args.Message);
      receivedMessage.IsSuccessfullyReceived = true;
      receivedMessage.QueueName = args.EntityPath;
      receivedMessage.ServiceBusName = StringHelper.RemoveSbSuffix(args.FullyQualifiedNamespace);

      _logger.LogInformation("SbProcessor.MessageHandler(): Received the following message: {message}", JsonSerializer.Serialize(receivedMessage));

      // Transform the message's body in a JobRequest
      var jobRequest = JsonSerializer.Deserialize<JobRequest>(args.Message.Body.ToString());

      if (jobRequest == null)
      {
        _logger.LogError("SbProcessor.MessageHandler(): JobRequest is null. Message will be abandoned.");
        await args.AbandonMessageAsync(args.Message);
        return;
      }

      if (jobRequest.JobName.IsNullOrEmpty())
      {
        _logger.LogError("SbProcessor.MessageHandler(): The Job name is empty. Message will be abandoned.");
        await args.AbandonMessageAsync(args.Message);
        return;
      }

      var jobName = jobRequest.JobName;
      var jobNamespace = _config.GetValue<string>(ConfigKey.JobsNamespace);
      
      _logger.LogInformation("SbProcessor.MessageHandler(): Creating the Kubernetes Job {job}, in namespace: {namespace}.", jobName, jobNamespace);
      var result = await _k8SClient.CreateJobAsync(jobName, jobNamespace);

      if (result.IsSuccess)
      {
        _logger.LogInformation("SbProcessor.MessageHandler(): Kubernetes Job {job} created successfully. Message: {message}", jobName, result.ResultMessage);
        await args.CompleteMessageAsync(args.Message);
      }
      else
      {
        _logger.LogError("SbProcessor.MessageHandler(): Kubernetes Job {job} NOT created. Error: {error}", jobName, result.ResultMessage);
        await args.DeadLetterMessageAsync(args.Message);

        //TODO: Create code to process Dead-Lettered messages
      }

      // if we are here, something went wrong, so we abandon the message to not loose it (it will be retried!!!
      //TODO: Implement a retry mechanism with circuit breaker
      await args.AbandonMessageAsync(args.Message);
    }

    // handle any errors when receiving messages
    private Task ErrorHandler(ProcessErrorEventArgs args)
    {
      _logger.LogError("SbProcessor.ErrorHandler(): Message reception error {error}", args.Exception.ToString());
      return Task.CompletedTask;
    }

    private static ServiceBusProcessorOptions GetServiceBusProcessorOptions
    {
      get
      {
        var serviceBusProcessorOptions = new ServiceBusProcessorOptions
        {
          AutoCompleteMessages = false,
          MaxConcurrentCalls = 1,
          MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(10),
          ReceiveMode = ServiceBusReceiveMode.PeekLock
        };
        return serviceBusProcessorOptions;
      }
    }

    #endregion
  }
}
