using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using ListenerAPI.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ListenerAPI.Classes
{
  public class SbBatchSender : ISbBatchSender, IAsyncDisposable
  {
    private readonly ILogger<SbBatchSender> _logger;
    private ServiceBusClient? _sbClient;
    private readonly IConfiguration _config;

    public SbBatchSender(
      ILogger<SbBatchSender> logger,
      IConfiguration config
    )
    {
      _logger = logger;
      _config = config;
      _logger.LogInformation("SbBatchSender constructed");

      CreateClient();
    }

    #region ISbBatchSender implementation
    private void CreateClient()
    {
      _logger.LogInformation("SbBatchSender.CreateClient() called");

      var sbConnString = _config.GetValue<string>("azSbPrimaryConnString");
      _logger.LogDebug($"Found in config: \"azSbPrimaryConnString\": \"{sbConnString}\"");

      // Creating the Service Bus client:
      // Ref: https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-dotnet-get-started-with-queues?tabs=connection-string

      // Use the connection string to create a client.
      try
      {
        var clientOptions = new ServiceBusClientOptions()
        {
          TransportType = ServiceBusTransportType.AmqpWebSockets
        };
        _sbClient = new ServiceBusClient(sbConnString, clientOptions);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex.ToString());
      }
    }
    
    public async Task SendMessagesAsync(int numOfMessages)
    {
      _logger.LogInformation("SbBatchSender.SendMessagesAsync() called");

      if (_sbClient == null)
      {
        _logger.LogError("Service Bus client is not created");
        throw new Exception($"The ServiceBus client should be created before sending messages.");
      }

      var sbQueueSenderName = _config.GetValue<string>("azSbQueueName");
      _logger.LogDebug($"Found in config: \"azSbQueueName\": \"{sbQueueSenderName}\"");

      var sender = _sbClient.CreateSender(sbQueueSenderName);

      // create a batch to send multiple messages
      using var messageBatch = await sender.CreateMessageBatchAsync();

      for (var i = 1; i <= numOfMessages; i++)
      {
        // try adding a message to the batch
        if (!messageBatch.TryAddMessage(new ServiceBusMessage($"Message {i}")))
        {
          // if it is too large for the batch
          throw new Exception($"The message {i} is too large to fit in the batch.");
        }
      }

      try
      {
        // Use the producer client to send the batch of messages to the Service Bus queue
        await sender.SendMessagesAsync(messageBatch);
        _logger.LogInformation($"A batch of {numOfMessages} messages has been published to the queue.");
      }
      finally
      {
        // Calling DisposeAsync on client types is required to ensure that network
        // resources and other unmanaged objects are properly cleaned up.
        await sender.DisposeAsync();
      }
    }

    public async ValueTask DisposeAsync()
    {
      _logger.LogDebug("SbBatchSender.DisposeAsync() called");

      if (_sbClient != null)
      {
        await _sbClient.DisposeAsync();
      }
    }
    #endregion
  }
}