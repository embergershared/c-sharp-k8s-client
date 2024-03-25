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

namespace ListenerAPI.Controllers
{
  [ApiController]
  [Route("api/[controller]")]

  public class Queues : Controller
  {
    private readonly ILogger<Queues> _logger;
    private readonly IConfiguration _config;
    private readonly IServiceBusQueues _sbQueues;
    
    public Queues(
      ILogger<Queues> logger,
      IConfiguration config,
      IServiceBusQueues sbQueues
    )
    {
      _logger = logger;
      _config = config;
      _sbQueues = sbQueues ?? throw new ArgumentNullException(nameof(sbQueues));

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

      // We retrieve X messages from all queues in all Service Bus namespaces
      var receiveMessagesTasks = new List<Task<IReadOnlyList<ReceivedMessage>>>();
      foreach (var sbNamespace in AppGlobal.GetServiceBusNames(_config))
      {
        await _sbQueues.AddReceiveMessageBatchFromQueuesTasksAsync(sbNamespace!, receiveMessagesTasks, count);
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

      var tasks = new List<Task<int>>();
      foreach (var sbNamespace in AppGlobal.GetServiceBusNames(_config))
      {
        await _sbQueues.AddSenderToQueuesTasksAsync(value, sbNamespace!, tasks);
      }

      var results = await Task.WhenAll(tasks);

      return results.Sum() switch
      {
        > 0 => CreatedAtAction(nameof(Post), value),
        0 => StatusCode(StatusCodes.Status204NoContent, "No messages created"),
        _ => StatusCode(StatusCodes.Status500InternalServerError, "Error Sending messages")
      };
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

      try
      {
        // We delete all messages from all queues in all Service Bus namespaces, without keeping messages'content
        var deleteAllTasks = new List<Task<int>>();
        foreach (var sbNamespace in AppGlobal.GetServiceBusNames(_config))
        {
          await _sbQueues.AddDeleteAllFromQueuesTasksAsync(sbNamespace!, deleteAllTasks);
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


    // Trace Controller disposal
    protected override void Dispose(bool disposing)
    {
      _logger.LogDebug("Queues.Dispose() called");
      if (disposing)
      {
        //sbSender.DisposeClientAsync();
      }
      base.Dispose(disposing);
    }
  }
}
