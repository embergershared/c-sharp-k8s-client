using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using ListenerAPI.Classes;
using ListenerAPI.Constants;
using ListenerAPI.Helpers;
using ListenerAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace ListenerAPI.Controllers
{
  [ApiController]
  [Route("api/[controller]")]

  public class Queues : Controller
  {
    private readonly ILogger<Queues> _logger;
    private readonly IConfiguration _config;
    private readonly IAzureClientFactory<ServiceBusClient> _sbClientFactory;

    public Queues(
      ILogger<Queues> logger,
      IConfiguration config,
      IAzureClientFactory<ServiceBusClient> sbClientFactory
    )
    {
      _logger = logger;
      _config = config;
      _sbClientFactory = sbClientFactory ?? throw new ArgumentNullException(nameof(sbClientFactory));

      _logger.LogDebug("Controllers/Queues constructed");
    }

    // GET api/queues
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Get(int count = 1)
    {
      _logger.LogInformation("HTTP GET /api/Queues/{count} called", count);

      if (!bool.Parse(AppGlobal.Data["IsUsingServiceBus"]))
        return NotFound("Error: No ServiceBus(es) set in configuration to RECEIVE messages FROM");

      var sbNamespaces = Const.SbNamesConfigKeyNames
        .Select(key => _config[key])
        .Where(sb => !string.IsNullOrEmpty(sb)).ToList();

      // We retrieve X messages from all queues in all Service Bus namespaces
      var receiveMessagesTasks = new List<Task<IReadOnlyList<ReceivedMessage>>>();
      foreach (var sbNamespace in sbNamespaces)
      {
        await AddReceiveMessageBatchFromQueuesTasksAsync(sbNamespace!, receiveMessagesTasks, count);
      }

      // Execute the tasks on all the queues in parallel
      var receivedResults = await Task.WhenAll(receiveMessagesTasks);

      // Check if all messages from all queues were successfully received
      if (!receivedResults.All(predicate: r => r.All(m => m.IsSuccessfullyReceived)))
        return StatusCode(StatusCodes.Status500InternalServerError, "Error Receiving messages");

      // Generate the Http response
      foreach (var result in receivedResults)
      {
        var resultJson = JsonSerializer.Serialize(result);
        _logger.LogInformation("Received message: {content}", resultJson);
      }
      return Ok(JsonSerializer.Serialize(receivedResults));
    }

    // POST api/queues
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Post([FromBody] int value)
    {
      _logger.LogInformation("HTTP POST /api/Queues called with {value} in body", value);

      if (!bool.Parse(AppGlobal.Data["IsUsingServiceBus"]))
        return NotFound("Error: No ServiceBus(es) set in configuration to SEND messages TO");

      var sbNamespaces = new List<string>();
      foreach (var key in Const.SbNamesConfigKeyNames)
      {
        var sb = _config[key];
        if (!string.IsNullOrEmpty(sb))
        {
          sbNamespaces.Add(sb);
        }
      }

      var tasks = new List<Task<IActionResult>>();
      foreach (var sbNamespace in sbNamespaces)
      {
        await AddSenderToQueuesTasks(value, sbNamespace, tasks);
      }

      var results = await Task.WhenAll(tasks);

      if (results.All(predicate: x => x is CreatedAtActionResult))
      {
        return CreatedAtAction(nameof(Post), value);
      }

      return StatusCode(StatusCodes.Status500InternalServerError, "Error Sending messages");
    }
    
    // DELETE api/queues
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete()
    {
      _logger.LogInformation("HTTP DELETE /api/Queues called");

      if (!bool.Parse(AppGlobal.Data["IsUsingServiceBus"]))
        return NotFound("Error: No ServiceBus(es) set in configuration to RECEIVE messages FROM");

      var sbNamespaces = Const.SbNamesConfigKeyNames
        .Select(key => _config[key])
        .Where(sb => !string.IsNullOrEmpty(sb)).ToList();

      try
      {
        // We delete all messages from all queues in all Service Bus namespaces, without keeping messages'content
        var deleteAllTasks = new List<Task<int>>();
        foreach (var sbName in sbNamespaces)
        {
          var queuesNames = await GetQueueNamesAsync(sbName!);
          deleteAllTasks.AddRange(queuesNames.Select(queue => DeleteAllMessagesAsync(sbName!, queue)));
        }

        // Execute the DeleteAll tasks on all the queues in parallel
        var deleteAllResults = await Task.WhenAll(deleteAllTasks);

        // Generate the Http response
        return Ok($"Deleted a total of {deleteAllResults.Sum()} messages.");
      }
      catch (Exception ex)
      {
        _logger.LogError("Called failed with exception: {ex}", ex);
        return StatusCode(StatusCodes.Status500InternalServerError, "Error Deleting messages");
      }
    }


    #region Private Methods
    // Send X messages in a batch to all queue(s) in all Service Bus namespace(s)
    private async Task AddSenderToQueuesTasks(int value, string sbName, List<Task<IActionResult>> tasks)
    {
      _logger.LogDebug("Queues.AddSenderToQueuesTasks({value}, {sbName}, tasks) started", value, sbName);

      var queuesNames = await GetQueueNamesAsync(sbName);
      tasks.AddRange(queuesNames.Select(queue =>
        SendMessageBatchToQueueAsync(_sbClientFactory.CreateClient(sbName).CreateSender(queue), value)));
    }
    private async Task<IActionResult> SendMessageBatchToQueueAsync(ServiceBusSender sender, int value)
    {
      _logger.LogDebug("Queues.SendMessageBatchToQueueAsync({sender}, {value}) started", sender.Identifier, value);

      // create a batch to send multiple messages
      using var messageBatch = await sender.CreateMessageBatchAsync();

      for (var i = 1; i <= value; i++)
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
        {
          await sender.SendMessagesAsync(messageBatch);

          return CreatedAtAction(nameof(Post), value);
        }
      }
      catch (Exception ex)
      {
        _logger.LogError("Called failed with exception: {ex}", ex);

        return StatusCode(StatusCodes.Status500InternalServerError,
          "Error Sending messages");
      }
    }

    //// Receive 1 message from all queue(s) in all Service Bus namespace(s)
    //private async Task AddReceiveMessageFromQueuesTasksAsync(string sbName, List<Task<ReceivedMessage>> tasks)
    //{
    //  _logger.LogDebug("Queues.AddReceiveMessageFromQueuesTasksAsync({sbName}, tasks) started", sbName);

    //  var queuesNames = await GetQueueNamesAsync(sbName);
    //  tasks.AddRange(queuesNames.Select(queue =>
    //    ReceiveMessageFromQueueAsync(_sbClientFactory.CreateClient(sbName).CreateReceiver(queue))));
    //}
    //private async Task<ReceivedMessage> ReceiveMessageFromQueueAsync(ServiceBusReceiver receiver)
    //{
    //  _logger.LogDebug("Queues.ReceiveMessageFromQueueAsync({receiver}) started", receiver.Identifier);
    //  var message = new ReceivedMessage
    //  {
    //    ServiceBusName = StringHelper.RemoveSbSuffix(receiver.FullyQualifiedNamespace),
    //    QueueName = receiver.EntityPath
    //  };

    //  try
    //  {
    //    {
    //      // the received message is a different type as it contains some service set properties
    //      var sbReceivedMessage = await receiver.ReceiveMessageAsync();

    //      message.IsSuccessfullyReceived = true;
    //      message.Body = sbReceivedMessage.Body.ToString();
    //      message.SeqNumber = sbReceivedMessage.SequenceNumber;
    //      message.MessageId = sbReceivedMessage.MessageId;

    //      // Complete the message: Delete it from the queue
    //      await receiver.CompleteMessageAsync(sbReceivedMessage);

    //      // ### Other Received message actions ###
    //      // Ref: https://learn.microsoft.com/en-us/dotnet/api/overview/azure/messaging.servicebus-readme?view=azure-dotnet#complete-a-message
    //      //// Abandon the message: Release lock, allowing it to be picked again
    //      //await receiver.AbandonMessageAsync(sbReceivedMessage);

    //      //// Defer the message: Move it to Deferred state to be picked by ReceiveDeferredMessageAsync()
    //      //await receiver.DeferMessageAsync(sbReceivedMessage);

    //      //// Dead-letter the message: Move it to the Dead-letter queue
    //      //await receiver.DeadLetterMessageAsync(sbReceivedMessage,"reason", "description");
    //    }
    //  }
    //  catch (Exception ex)
    //  {
    //    _logger.LogError("Called failed with exception: {ex}", ex);
    //  }

    //  return message;
    //}

    // Receive a batch of X messages from all queue(s) in all Service Bus namespace(s)
    private async Task AddReceiveMessageBatchFromQueuesTasksAsync(string sbName, List<Task<IReadOnlyList<ReceivedMessage>>> tasks, int batchSize = 1)
    {
      _logger.LogDebug("Queues.AddReceiveMessageFromQueuesTasksAsync({sbName}, tasks) started", sbName);

      var queuesNames = await GetQueueNamesAsync(sbName);
      tasks.AddRange(queuesNames.Select(queue =>
        ReceiveMessageBatchFromQueueAsync(_sbClientFactory.CreateClient(sbName).CreateReceiver(queue), batchSize)
      ));
    }
    private async Task<IReadOnlyList<ReceivedMessage>> ReceiveMessageBatchFromQueueAsync(ServiceBusReceiver receiver, int batchSize = 1)
    {
      _logger.LogDebug("Queues.ReceiveMessageBatchFromQueueAsync({receiver}, {size}) started", receiver.Identifier, batchSize);

      var messages = new List<ReceivedMessage>();

      try
      {
        {
          // Receive the Batch of messages
          var sbReceivedMessages = await receiver.ReceiveMessagesAsync(maxMessages: batchSize);

          foreach (var sbReceivedMessage in sbReceivedMessages)
          {
            var receivedMessage = new ReceivedMessage
            {
              IsSuccessfullyReceived = true,
              ServiceBusName = StringHelper.RemoveSbSuffix(receiver.FullyQualifiedNamespace),
              QueueName = receiver.EntityPath,
              Body = sbReceivedMessage.Body.ToString(),
              SeqNumber = sbReceivedMessage.SequenceNumber,
              MessageId = sbReceivedMessage.MessageId
            };
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

    // Delete all messages from a queue
    [NonAction]
    public async Task<int> DeleteAllMessagesAsync(string sbName, string queue)
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


    // Get all queue(s) for a Service Bus namespace
    private async Task<List<string>> GetQueueNamesAsync(string serviceBusName)
    {
      _logger.LogDebug("Queues.GetQueueNamesAsync({serviceBusName}) started", serviceBusName);

      // Query the available queues for the Service Bus namespace.
      var adminClient = new ServiceBusAdministrationClient
        ($"{serviceBusName}{Const.SbPublicSuffix}",
          AzureCreds.GetCred(_config.GetValue<string>("PreferredAzureAuth"))
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
    #endregion

    // Trace Controller disposal
    protected override void Dispose(bool disposing)
    {
      _logger.LogDebug("Queues.Dispose() started");
      if (disposing)
      {
        //sbSender.DisposeClientAsync();
      }
      base.Dispose(disposing);
    }
  }
}
