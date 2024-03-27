using ListenerAPI.Classes;
using ListenerAPI.Interfaces;
using ListenerAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ListenerAPI.Constants;

namespace ListenerAPI.Controllers
{
  [ApiController]
  [Route("api/[controller]")]

  public class Messages : Controller
  {
    private readonly ILogger<Messages> _logger;
    private readonly IConfiguration _config;
    private readonly ISbMessages _sbMessages;
    
    public Messages(
      ILogger<Messages> logger,
      IConfiguration config,
      ISbMessages sbMessages
    )
    {
      _logger = logger;
      _config = config;
      _sbMessages = sbMessages ?? throw new ArgumentNullException(nameof(sbMessages));

      _logger.LogDebug("Controllers/Messages constructed");
    }

    // GET api/messages
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Get(int count = 1)
    {
      _logger.LogInformation("HTTP GET /api/Messages/{count} called", count);

      if (!bool.Parse(AppGlobal.Data["IsUsingServiceBus"]))
        return NotFound("Error: No ServiceBus(es) set in configuration to RECEIVE messages FROM");

      // We retrieve X messages from all queues in all Service Bus namespaces
      var receiveMessagesTasks = new List<Task<IReadOnlyList<ReceivedMessage>>>();
      foreach (var sbNamespace in AppGlobal.GetServiceBusNames(_config))
      {
        await _sbMessages.AddReceiveMessagesBatchesFrom1NsAllQueuesTasksAsync(sbNamespace!, receiveMessagesTasks, count);
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

    // POST api/messages
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Post(int messagesToCreateCount, string? nsQueueName)
    {
      _logger.LogInformation("HTTP POST /api/Messages called with {messagesToCreateCount} messages to create", messagesToCreateCount);

      if (messagesToCreateCount < 1)
        return BadRequest("Error: The messages to create count must be greater than 0");

      if (!bool.Parse(AppGlobal.Data["IsUsingServiceBus"]))
        return NotFound("Error: No ServiceBus(es) set in configuration to SEND messages TO");

      var sendMessagesTasks = new List<Task<int>>();

      if (string.IsNullOrEmpty(nsQueueName))
      {
        foreach (var sbNamespace in AppGlobal.GetServiceBusNames(_config))
        {
          await _sbMessages.AddSendMessagesTo1NsAllQueuesTasksAsync(messagesToCreateCount, sbNamespace!, sendMessagesTasks);
        }
      }
      else
      {
        var sbNsQueue = AppGlobal.GetNames(nsQueueName);
        if (sbNsQueue.SbNamespace != null && sbNsQueue.QueueName != null)
        {
          _sbMessages.AddSendMessagesTo1Ns1QueueTask(
            messagesToCreateCount, 
            sbNsQueue.SbNamespace, 
            sbNsQueue.QueueName, 
            sendMessagesTasks);
        }
        else
        {
          return NotFound($"Error: The ServiceBus Namespace/Queue: {nsQueueName} is not valid");
        }
      }

      if (sendMessagesTasks.Count == 0)
        return NotFound("Error: No ServiceBus(es)/Queue(s) to SEND messages TO found");

      var results = await Task.WhenAll(sendMessagesTasks);

      return results.Sum() switch
      {
        > 0 => StatusCode(StatusCodes.Status201Created, $"Created {messagesToCreateCount} new message(s)"),
        0 => StatusCode(StatusCodes.Status204NoContent, "No messages created"),
        _ => StatusCode(StatusCodes.Status500InternalServerError, "Error Sending messages")
      };
    }

    // DELETE api/messages
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(string? nsQueueName)
    {
      _logger.LogInformation("HTTP DELETE /api/Messages called");

      if (!bool.Parse(AppGlobal.Data["IsUsingServiceBus"]))
        return NotFound("Error: No ServiceBus(es) set in configuration to DELETE messages FROM");

      try
      {
        // We delete all messages from all queues in all Service Bus namespaces, without keeping messages' content
        var deleteAllMessagesTasks = new List<Task<int>>();

        if (string.IsNullOrEmpty(nsQueueName))
        {
          foreach (var sbNamespace in AppGlobal.GetServiceBusNames(_config))
          {
            await _sbMessages.AddDeleteAllMessagesFrom1NsAllQueuesTasksAsync(sbNamespace!, deleteAllMessagesTasks);
          }
        }
        else
        {
          var sbNsQueue = AppGlobal.GetNames(nsQueueName);
          if (sbNsQueue.SbNamespace != null && sbNsQueue.QueueName != null)
          {
            _sbMessages.AddDeleteAllMessagesFrom1Ns1QueueTask(
              sbNsQueue.SbNamespace,
              sbNsQueue.QueueName,
              deleteAllMessagesTasks
            );
          }
          else
          {
            return NotFound($"Error: The ServiceBus Namespace/Queue: {nsQueueName} is not valid");
          }
        }

        if (deleteAllMessagesTasks.Count == 0)
          return NotFound("Error: No ServiceBus(es)/Queue(s) to DELETE messages FROM found");
        
        // Execute the DeleteAll tasks on all the queues in parallel
        var deleteAllResults = await Task.WhenAll(deleteAllMessagesTasks);

        // Generate the Http response
        return Ok($"Deleted a total of {deleteAllResults.Sum()} messages.");
      }
      catch (Exception ex)
      {
        _logger.LogError("Called failed with exception: {ex}", ex);
        return StatusCode(StatusCodes.Status500InternalServerError, "Error Deleting messages");
      }
    }


    // Trace Controller disposal
    protected override void Dispose(bool disposing)
    {
      _logger.LogDebug("Messages.Dispose() called");
      if (disposing)
      {
        //sbSender.DisposeClientAsync();
      }
      base.Dispose(disposing);
    }
  }
}
