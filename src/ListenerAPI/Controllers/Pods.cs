using ListenerAPI.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ListenerAPI.Controllers
{
  [ApiController]
    [Route("api/[controller]")]
    public class Pods : ControllerBase
    {
        private readonly ILogger<Pods> _logger;
        private readonly IK8SClient _k8SClient;

        public Pods(
            ILogger<Pods> logger,
            IK8SClient k8SClient
            )
        {
            _logger = logger;
            _k8SClient = k8SClient;
            _logger.LogInformation("Controllers/Pods constructed");
        }

        // GET: api/<PodsController>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> Get()
        {
            _logger.LogInformation("HTTP GET /api/Pods called");

            try
            {
                var podsList = await _k8SClient.GetPodsAsync();
                return Ok(podsList);
            }
            catch (Exception ex)
            {
              _logger.LogError("Called failed with exception: {ex}", ex);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "Error retrieving the pods");
            }
        }
    }
}
