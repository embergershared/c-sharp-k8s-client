using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using ListenerAPI.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

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
      _sbClientFactory = sbClientFactory;
      
      _logger.LogDebug("Controllers/Queues constructed");
    }

    // POST api/<QueuesController>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> Put([FromBody] int value)
    {
      _logger.LogInformation("HTTP POST /api/Queues called with {value} in body", value);

      var sbNamespaces = new List<string>
      {
        _config["ServiceBusMainName"] ?? string.Empty,
        _config["ServiceBusSecondaryName"] ?? string.Empty
      };

      var tasks = new List<Task<ActionResult>>();

      if (sbNamespaces.Count == 0)
        return StatusCode(StatusCodes.Status500InternalServerError,
          "Error: No Service Bus Namespaces to send to defined");

      foreach (var sbNamespace in sbNamespaces)
      {
        await AddSenderToQueuesTasks(value, sbNamespace, tasks);
      }

      var results = await Task.WhenAll(tasks);

      if (results.All(predicate: x => x is CreatedAtActionResult))
      {
        return CreatedAtAction(nameof(Put), value);
      }

      return StatusCode(StatusCodes.Status500InternalServerError,
        "Error Sending messages");
    }

    private async Task AddSenderToQueuesTasks(int value, string sbName, List<Task<ActionResult>> tasks)
    {
      var queuesNames = await GetQueueNames(sbName);
      tasks.AddRange(queuesNames.Select(queue =>
        ActionResultAsync(_sbClientFactory.CreateClient(sbName).CreateSender(queue), value)));
    }
    
    private async Task<ActionResult> ActionResultAsync(ServiceBusSender sender, int value)
    {
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

    protected override void Dispose(bool disposing)
    {
      _logger.LogDebug("Controllers/Queues disposing");
      if (disposing)
      {
        //sbSender.DisposeClientAsync();
      }
      base.Dispose(disposing);
    }

    private static async Task<List<string>> GetQueueNames(string serviceBusName)
    {
      // Query the available queues for the Service Bus namespace.
      var adminClient = new ServiceBusAdministrationClient
        ($"{serviceBusName}{Const.SbPublicSuffix}", new DefaultAzureCredential());
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


  }
}
