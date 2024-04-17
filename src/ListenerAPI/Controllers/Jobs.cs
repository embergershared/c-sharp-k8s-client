using ListenerAPI.Constants;
using ListenerAPI.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace ListenerAPI.Controllers
{
  [ApiController]
    [Route("api/[controller]")]
    public class Jobs : ControllerBase
    {
        private readonly ILogger<Jobs> _logger;
        private readonly IK8SClient _k8SClient;
        private readonly IConfiguration _config;

        public Jobs(
            ILogger<Jobs> logger,
            IConfiguration config,
            IK8SClient k8SClient
        )
        {
            _logger = logger;
            _config = config;
            _k8SClient = k8SClient;
            _logger.LogInformation("Controllers/Jobs constructed");
        }

        // POST api/<JobsController>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]

        public async Task<ActionResult<string>> PostCreateJob(string jobName)
        {
            _logger.LogInformation("HTTP POST /api/Jobs called");

            try
            {
              var result = await _k8SClient.CreateJobAsync(jobName, _config.GetValue<string>(ConfigKey.JobsNamespace));

              return StatusCode(result.IsSuccess ? StatusCodes.Status201Created : StatusCodes.Status400BadRequest, result.ResultMessage);
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
