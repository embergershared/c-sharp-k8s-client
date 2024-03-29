using ListenerAPI.Constants;
using ListenerAPI.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using ListenerAPI.Models;

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

        public async Task<ActionResult<string>> PostCreateJob(string JobName)
        {
            _logger.LogInformation("HTTP POST /api/Jobs called");

            try
            {
                var result = await _k8SClient.CreateJobAsync(JobName, Const.K8SNsName);
        return StatusCode(StatusCodes.Status201Created, $"Created job.batch/{result.JobName} in namespace {result.JobNamespaceName} at {result.JobCreationTime.Value.LocalDateTime} with image {result.JobContainerImage} and nodeSelector {result.JobNodeSelector}");

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
