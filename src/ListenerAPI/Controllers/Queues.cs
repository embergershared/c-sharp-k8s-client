using System;
using System.Linq;
using System.Threading.Tasks;
using ListenerAPI.Factories;
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
    private readonly IConfiguration _config;
    private readonly IAbstractFactory<ISbSender> _sbSenderFactory;
    private readonly IAbstractFactory<ISbClient> _sbClientFactory;

    public Queues(
      ILogger<Queues> logger,
      IConfiguration config,
      IAbstractFactory<ISbClient> sbClientFactory,
      IAbstractFactory<ISbSender> sbSenderFactory
    )
    {
      _logger = logger;
      _config = config;
      _sbClientFactory = sbClientFactory;
      _sbSenderFactory = sbSenderFactory;
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

      var tasks = new[]
      {
        ActionResultAsync(value, "01"),
        ActionResultAsync(value, "02")
      };

      var results = await Task.WhenAll(tasks);

      if (results.All(predicate: x => x is CreatedAtActionResult))
      {
        return CreatedAtAction(nameof(Put), value);
      }

      return StatusCode(StatusCodes.Status500InternalServerError,
        "Error Sending messages");
    }


    private async Task<ActionResult> ActionResultAsync(int value, string sbNum)
    {
      var connStringConfigKey = $"azSb{sbNum}PrimaryConnString";
      var queueNameConfigKey = $"azSb{sbNum}QueueName";

      var sbConnString = _config.GetValue<string>(connStringConfigKey);
      _logger.LogDebug("Found in config: \"{connStringConfigKey}\": \"{sbConnString}\"", connStringConfigKey, sbConnString);

      var sbQueueSenderName = _config.GetValue<string>(queueNameConfigKey);
      _logger.LogDebug("Found in config: \"{queueNameConfigKey}\": \"{sbQueueSenderName}\"", queueNameConfigKey, sbQueueSenderName);

      if (sbConnString == null || sbQueueSenderName == null)
      {
        _logger.LogWarning("Missing configuration values for {connStringConfigKey} / {queueNameConfigKey} to send messages", connStringConfigKey, queueNameConfigKey);
        return StatusCode(StatusCodes.Status500InternalServerError,
          "Missing configuration to send messages");
      }

      try
      {
        {
          var sbClient = _sbClientFactory.Create().CreateClientCS(sbConnString);
          if (sbClient != null) await _sbSenderFactory.Create().SendMessagesAsync(sbClient, sbQueueSenderName, value);

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
  }
}
