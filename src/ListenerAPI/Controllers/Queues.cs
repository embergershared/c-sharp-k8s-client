using System;
using System.Threading.Tasks;
using ListenerAPI.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ListenerAPI.Controllers
{
  [ApiController]
  [Route("api/[controller]")]

  public class Queues : Controller
  {
    private readonly ILogger<Queues> _logger;
    private readonly ISbSender _sbSender;
    private readonly Func<ISbClient> _sbClientFactory;
    private readonly IConfiguration _config;

    public Queues(
      ILogger<Queues> logger,
      IConfiguration config,
      Func<ISbClient> sbClientFactory,
      ISbSender sbSender
    )
    {
      _logger = logger;
      _config = config;
      _sbClientFactory = sbClientFactory;
      _sbSender = sbSender;
      _logger.LogInformation("Controllers/Queues constructed");
    }

    protected override void Dispose(bool disposing)
    {
      _logger.LogInformation("Controllers/Queues disposing");
      if (disposing)
      {
        //sbSender.DisposeClientAsync();
      }
      base.Dispose(disposing);
    }

    // POST api/<QueuesController>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]

    public async Task<ActionResult> Put([FromBody] int value)
    {
      _logger.LogInformation("HTTP POST /api/Queues called with {value} in body", value);

      var sbConnString = _config.GetValue<string>("azSbPrimaryConnString");
      _logger.LogDebug("Found in config: \"azSbPrimaryConnString\": \"{sbConnString}\"", sbConnString);

      var sbQueueSenderName = _config.GetValue<string>("azSbQueueName");
      _logger.LogDebug("Found in config: \"azSbQueueName\": \"{sbQueueSenderName}\"", sbQueueSenderName);

      if (sbConnString != null && sbQueueSenderName != null)
      {
        try
        {
          {
            var sbClient = _sbClientFactory().CreateClientCS(sbConnString);
            if (sbClient != null) await _sbSender.SendMessagesAsync(sbClient, sbQueueSenderName, value);

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
      else
      {
        return StatusCode(StatusCodes.Status500InternalServerError,
          "Missing configuration to send messages");
      }
    }
  }
}
