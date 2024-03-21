using System;
using System.Threading.Tasks;
using ListenerAPI.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ListenerAPI.Controllers
{
  [ApiController]
  [Route("api/[controller]")]

  public class Queues : Controller
  {
    private readonly ILogger<Queues> _logger;
    private readonly ISbBatchSender _sbBatchSender;

    public Queues(
      ILogger<Queues> logger,
      ISbBatchSender sbBatchSender
    )
    {
      _logger = logger;
      _sbBatchSender = sbBatchSender;
      _logger.LogInformation("Controllers/Queues constructed");
    }

    protected override void Dispose(bool disposing)
    {
      _logger.LogInformation("Controllers/Queues disposing");
      if (disposing)
      {
        //sbBatchSender.DisposeClientAsync();
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
      _logger.LogInformation($"HTTP POST /api/Queues called with {value} in body");

      try
      {
        await _sbBatchSender.SendMessagesAsync(value);
        //return StatusCode(StatusCodes.Status201Created, "Job created");

        return CreatedAtAction(nameof(Put), value);
      }
      catch (Exception ex)
      {
        _logger.LogError($"Called failed with exception: {ex}");

        return StatusCode(StatusCodes.Status500InternalServerError,
          "Error creating the job");
      }
    }
  }
}
