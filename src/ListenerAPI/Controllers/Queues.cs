using Azure.Identity;
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
    public async Task<IActionResult> Get()
    {
      _logger.LogInformation("HTTP GET /api/Queues called");

      if (!bool.Parse(AppGlobal.Data["IsUsingServiceBus"]))
        return NotFound("Error: No ServiceBus(es) set in configuration to RECEIVE messages FROM");

      var sbNamespaces = Const.SbNamesKeys
        .Select(key => _config[key])
        .Where(sb => !string.IsNullOrEmpty(sb)).ToList();

      var tasks = new List<Task<ReceivedMessage>>();
      foreach (var sbNamespace in sbNamespaces)
      {
        await AddReceiverFromQueuesTasks(sbNamespace!, tasks);
      }

      var results = await Task.WhenAll(tasks);

      if (!results.All(predicate: x => x.IsSuccessfullyReceived))
        return StatusCode(StatusCodes.Status500InternalServerError, "Error Receiving messages");

      foreach (var result in results)
      {
        var resultJson = JsonSerializer.Serialize(result);
        _logger.LogInformation("Received message: {content}", resultJson);
      }
      return Ok(JsonSerializer.Serialize(results));
    }

    // POST api/queues
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Put([FromBody] int value)
    {
      _logger.LogInformation("HTTP POST /api/Queues called with {value} in body", value);

      if (!bool.Parse(AppGlobal.Data["IsUsingServiceBus"]))
        return NotFound("Error: No ServiceBus(es) set in configuration to SEND messages TO");

      var sbNamespaces = new List<string>();
      foreach (var key in Const.SbNamesKeys)
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
        return CreatedAtAction(nameof(Put), value);
      }

      return StatusCode(StatusCodes.Status500InternalServerError, "Error Sending messages");
    }
    
    #region Private Methods
    // Send X messages in a batch to all queue(s) in all Service Bus namespace(s)
    private async Task AddSenderToQueuesTasks(int value, string sbName, List<Task<IActionResult>> tasks)
    {
      _logger.LogDebug("Queues.AddSenderToQueuesTasks({value}, {sbName}, tasks) started", value, sbName);

      var queuesNames = await GetQueueNames(sbName);
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

          return CreatedAtAction(nameof(Put), value);
        }
      }
      catch (Exception ex)
      {
        _logger.LogError("Called failed with exception: {ex}", ex);

        return StatusCode(StatusCodes.Status500InternalServerError,
          "Error Sending messages");
      }
    }

    // Receive 1 message from all queue(s) in all Service Bus namespace(s)
    private async Task AddReceiverFromQueuesTasks(string sbName, List<Task<ReceivedMessage>> tasks)
    {
      _logger.LogDebug("Queues.AddReceiverFromQueuesTasks({sbName}, tasks) started", sbName);

      var queuesNames = await GetQueueNames(sbName);
      tasks.AddRange(queuesNames.Select(queue =>
        ReceiveMessageFromQueueAsync(_sbClientFactory.CreateClient(sbName).CreateReceiver(queue))));
    }
    private async Task<ReceivedMessage> ReceiveMessageFromQueueAsync(ServiceBusReceiver receiver)
    {
      _logger.LogDebug("Queues.ReceiveMessageFromQueueAsync({receiver}) started", receiver.Identifier);
      var message = new ReceivedMessage
      {
        ServiceBusName = StringHelper.RemoveSbSuffix(receiver.FullyQualifiedNamespace),
        QueueName = receiver.EntityPath
      };

      try
      {
        {
          // the received message is a different type as it contains some service set properties
          var sbReceivedMessage = await receiver.ReceiveMessageAsync();

          message.IsSuccessfullyReceived = true;
          message.Body = sbReceivedMessage.Body.ToString();
          message.SeqNumber = sbReceivedMessage.SequenceNumber;
          message.MessageId = sbReceivedMessage.MessageId;

          // complete the message, thereby deleting it from the service
          await receiver.CompleteMessageAsync(sbReceivedMessage);
        }
      }
      catch (Exception ex)
      {
        _logger.LogError("Called failed with exception: {ex}", ex);
      }

      return message;
    }

    // Get all queue(s) for a Service Bus namespace
    private async Task<List<string>> GetQueueNames(string serviceBusName)
    {
      _logger.LogDebug("Queues.GetQueueNames({serviceBusName}) started", serviceBusName);

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
