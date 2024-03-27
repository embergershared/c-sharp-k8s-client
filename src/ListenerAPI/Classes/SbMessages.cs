using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using ListenerAPI.Constants;
using ListenerAPI.Helpers;
using ListenerAPI.Interfaces;
using ListenerAPI.Models;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using System.Collections;

namespace ListenerAPI.Classes
{
  public class SbMessages : ISbMessages
  {
    private readonly ILogger<SbMessages> _logger;
    private readonly IConfiguration _config;
    private readonly IAzureClientFactory<ServiceBusClient> _sbClientFactory;
    private readonly IMapper _mapper;

    public SbMessages(
      ILogger<SbMessages> logger,
      IConfiguration config,
      IAzureClientFactory<ServiceBusClient> sbClientFactory,
      IMapper mapper
    )
    {
      _logger = logger;
      _config = config;
      _sbClientFactory = sbClientFactory ?? throw new ArgumentNullException(nameof(sbClientFactory));
      _mapper = mapper;
    }

    // Send X messages in a batch to all queue(s) in 1 Service Bus namespace
    public async Task AddSendMessagesTo1NsAllQueuesTasksAsync(int messagesCount, string sbName, List<Task<int>> tasks)
    {
      _logger.LogDebug("SbMessages.AddSenderToQueuesTasks({messagesCount}, {sbName}, tasks) called", messagesCount, sbName);

      var queuesNames = await GetAllQueuesNamesIn1NsAsync(sbName);
      tasks.AddRange(queuesNames.Select(queue =>
        SendMessageBatchToQueueAsync(_sbClientFactory.CreateClient(sbName).CreateSender(queue), messagesCount)));
    }
    private async Task<int> SendMessageBatchToQueueAsync(ServiceBusSender sender, int value)
    {
      _logger.LogDebug("SbMessages.SendMessageBatchToQueueAsync({sender}, {messagesCount}) called", sender.Identifier, value);

      // create a batch to send multiple messages
      using var messageBatch = await sender.CreateMessageBatchAsync();

      for (var i = 1; i <= value; i++)
      {
        // try adding a message to the batch
        if (!messageBatch.TryAddMessage(new ServiceBusMessage($"Body of message {i}.")))
        {
          // if it is too large for the batch
          throw new Exception($"The message {i} is too large to fit in the batch.");
        }
      }

      try
      {
        {
          await sender.SendMessagesAsync(messageBatch);

          return value;
        }
      }
      catch (Exception ex)
      {
        _logger.LogError("Called failed with exception: {ex}", ex);

        return -1;
      }
    }

    // Receive X messages in a batch from all queue(s) in 1 Service Bus namespace
    public async Task AddReceiveMessagesBatchesFrom1NsAllQueuesTasksAsync(string sbName, List<Task<IReadOnlyList<ReceivedMessage>>> tasks, int batchSize = 1)
    {
      _logger.LogDebug("SbMessages.AddReceiveMessageFromQueuesTasksAsync({sbName}, tasks) called", sbName);

      var queuesNames = await GetAllQueuesNamesIn1NsAsync(sbName);
      tasks.AddRange(queuesNames.Select(queue =>
        ReceiveMessageBatchFromQueueAsync(_sbClientFactory.CreateClient(sbName).CreateReceiver(queue), batchSize)
      ));
    }
    private async Task<IReadOnlyList<ReceivedMessage>> ReceiveMessageBatchFromQueueAsync(ServiceBusReceiver receiver, int batchSize = 1)
    {
      _logger.LogDebug("SbMessages.ReceiveMessageBatchFromQueueAsync({receiver}, {size}) called", receiver.Identifier, batchSize);

      var messages = new List<ReceivedMessage>();

      try
      {
        {
          // Receive the Batch of messages
          var sbReceivedMessages = await receiver.ReceiveMessagesAsync(maxMessages: batchSize);

          foreach (var sbReceivedMessage in sbReceivedMessages)
          {
            var receivedMessage = _mapper.Map<ReceivedMessage>(sbReceivedMessage);
            receivedMessage.IsSuccessfullyReceived = true;
            receivedMessage.ServiceBusName = StringHelper.RemoveSbSuffix(receiver.FullyQualifiedNamespace);
            receivedMessage.QueueName = receiver.EntityPath;

            messages.Add(receivedMessage);

            // Complete the message: Delete it from the queue
            await receiver.CompleteMessageAsync(sbReceivedMessage);

            // ### Other Received message actions ###
            // Ref: https://learn.microsoft.com/en-us/dotnet/api/overview/azure/messaging.servicebus-readme?view=azure-dotnet#complete-a-message
            //// Abandon the message: Release lock, allowing it to be picked again
            //await receiver.AbandonMessageAsync(sbReceivedMessage);

            //// Defer the message: Move it to Deferred state to be picked by ReceiveDeferredMessageAsync()
            //await receiver.DeferMessageAsync(sbReceivedMessage);

            //// Dead-letter the message: Move it to the Dead-letter queue
            //await receiver.DeadLetterMessageAsync(sbReceivedMessage,"reason", "description");
          }
        }
      }
      catch (Exception ex)
      {
        _logger.LogError("Called failed with exception: {ex}", ex);
      }

      return messages;
    }
    
    // Delete all messages from all queue(s) in 1 Service Bus namespace
    public async Task AddDeleteAllMessagesFrom1NsAllQueuesTasksAsync(string sbName, List<Task<int>> tasks)
    {
      _logger.LogDebug("SbMessages.AddDeleteAllMessagesFrom1NsAllQueuesTasksAsync({sbName}, tasks) called", sbName);

      var queuesNames = await GetAllQueuesNamesIn1NsAsync(sbName);
      tasks.AddRange(queuesNames.Select(queue => DeleteAllMessagesAsync(sbName, queue)));
    }
    private async Task<int> DeleteAllMessagesAsync(string sbName, string queue)
    {
      // Initialization
      const int maxMsg = 50;
      var maxWait = new TimeSpan(0, 0, 15);
      var haveMessagesToDelete = true;
      var deletedMessages = 0;

      _logger.LogInformation("Creating a ServiceBusReceiver for the queue: {@q_name}", queue);
      var receiver = _sbClientFactory.CreateClient(sbName).CreateReceiver(queue);
      if (receiver == null)
      {
        _logger.LogError("Error creating a Message receiver");
        return -1;
      }

      _logger.LogDebug("Retrieving messages by batches of {@max_msg}, with a {@max_wait} timeout", maxMsg, maxWait);
      while (haveMessagesToDelete)
      {
        _logger.LogDebug("Retrieving messages");
        var receivedMessages = await receiver.ReceiveMessagesAsync(maxMessages: maxMsg, maxWaitTime: maxWait);

        _logger.LogDebug("Processing response");
        if (receivedMessages.Any())
        {
          _logger.LogInformation("Received {@count} messages to delete", receivedMessages.Count);

          foreach (var receivedMessage in receivedMessages)
          {
            // get the message body as a string
            var body = receivedMessage.Body.ToString();
            _logger.LogInformation("Deleting message: {@body}", body);

            await receiver.CompleteMessageAsync(receivedMessage);
            deletedMessages++;
          }
          _logger.LogDebug("Deleted the retrieved messages");
        }
        else
        {
          haveMessagesToDelete = false;
        }
      }

      return deletedMessages;
    }

    // Get all queue(s) in 1 Service Bus namespace
    private async Task<List<string>> GetAllQueuesNamesIn1NsAsync(string serviceBusName)
    {
      _logger.LogDebug("SbMessages.GetAllQueuesNamesIn1NsAsync({serviceBusName}) called", serviceBusName);

      // Query the available queues for the Service Bus namespace.
      var adminClient = new ServiceBusAdministrationClient
      ($"{serviceBusName}{Const.SbPublicSuffix}",
        AzureCreds.GetCred(_config.GetValue<string>(Const.AzureIdentityPreferredConfigKeyName))
      );
      var queueNames = new List<string>();

      // Because the result is async, the queue names need to be captured
      // to a standard list to avoid async calls when registering. Failure to
      // do so results in an error with the services collection.
      await foreach (var queue in adminClient.GetQueuesAsync())
      {
        queueNames.Add(queue.Name);
      }

      return queueNames;
    }
    
    public ValueTask DisposeAsync()
    {
      _logger.LogDebug("SbMessages.DisposeAsync() called");

      GC.SuppressFinalize(this);

      return new ValueTask(Task.CompletedTask);
    }

    public bool AddSendMessagesTo1Ns1QueueTask(int messagesCount, string sbName, string qName, List<Task<int>> tasks)
    {
      try
      {
        tasks.Add(SendMessageBatchToQueueAsync(_sbClientFactory.CreateClient(sbName).CreateSender(qName), messagesCount));
        return true;
      }
      catch (Exception ex)
      {
        _logger.LogError("Error adding AddSendMessagesTo1Ns1QueueTask task: {ex}", ex);
        return false;
      }
    }

    public bool AddDeleteAllMessagesFrom1Ns1QueueTask(string sbName, string qName, List<Task<int>> tasks)
    {
      try
      {
        tasks.Add(DeleteAllMessagesAsync(sbName, qName));
        return true;
      }
      catch (Exception ex)
      {
        _logger.LogError("Error adding AddDeleteAllMessagesFrom1Ns1QueueTask task: {ex}", ex);
        return false;
      }
    }
  }
}
