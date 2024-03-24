using ListenerAPI.Constants;
using ListenerAPI.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace ListenerAPI.Controllers
{
  [ApiController]
    [Route("api/[controller]")]
    public class Jobs : ControllerBase
    {
        private readonly ILogger<Jobs> _logger;
        private readonly IK8SClient _k8SClient;

        public Jobs(
            ILogger<Jobs> logger,
            IK8SClient k8SClient
        )
        {
            _logger = logger;
            _k8SClient = k8SClient;
            _logger.LogInformation("Controllers/Jobs constructed");
        }

        // POST api/<JobsController>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]

        public async Task<ActionResult> Put([FromBody] string value)
        {
            _logger.LogInformation("HTTP POST /api/Jobs called");

            try
            {
                await _k8SClient.CreateJobAsync(value, Const.K8SNsName);
                //return StatusCode(StatusCodes.Status201Created, "Job created");
                return CreatedAtAction(nameof(Put), value);
            }
            catch (Exception ex)
            {
                _logger.LogError("Called failed with exception: {ex}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError,
                                       "Error creating the job");
            }
        }

    }
}
