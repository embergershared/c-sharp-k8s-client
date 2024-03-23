using System;
using System.Net;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using ListenerAPI.Interfaces;
using Microsoft.Extensions.Logging;

namespace ListenerAPI.Classes
{
  public class SbSender : ISbSender
  {
    private readonly ILogger<SbSender> _logger;
    private ServiceBusSender? _sbSender;

    public SbSender(
      ILogger<SbSender> logger
    )
    {
      _logger = logger;
      _logger.LogInformation("SbSender constructed");
    }

    #region ISbSender implementation
    
    public async Task SendMessagesAsync(ServiceBusClient sbClient, string queueName, int numOfMessages)
    {
      _logger.LogInformation("SbSender.SendMessagesAsync() called");

      if (sbClient == null)
      {
        _logger.LogError("ServiceBusClient is not created");
        throw new Exception($"The ServiceBusClient should be created before sending messages.");
      }

      try
      {
        _sbSender = sbClient.CreateSender(queueName);
        _logger.LogInformation("ServiceBusSender created: {@sbs_Id}", _sbSender.Identifier);
      }
      catch (Exception ex)
      {
        _logger.LogError("Error creating the ServiceBus Sender: {ex}", ex);
        throw;
      }

      // create a batch to send multiple messages
      using var messageBatch = await _sbSender.CreateMessageBatchAsync();

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
        // Use the sender client to send the batch of messages to the Service Bus queue
        await _sbSender.SendMessagesAsync(messageBatch);
        _logger.LogInformation("A batch of {numOfMessages} messages has been SentAsync to ServiceBus/Queue: {sbName}/{queueName}.", numOfMessages, _sbSender.FullyQualifiedNamespace, queueName);
      }
      finally
      {
        // Calling DisposeAsync on sender client types is required to ensure that network
        // resources and other unmanaged objects are properly cleaned up.
        await _sbSender.DisposeAsync();
      }
    }

    public async ValueTask DisposeAsync()
    {
      _logger.LogDebug("SbSender.DisposeAsync() called");

      if (_sbSender != null)
      {
        _logger.LogInformation("ServiceBusSender disposed: {@sb_Id}", _sbSender.Identifier);
        await _sbSender.DisposeAsync();
      }
      GC.SuppressFinalize(this);
    }
    #endregion
  }
}