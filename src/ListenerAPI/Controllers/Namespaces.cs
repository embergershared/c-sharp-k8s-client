using System;
using Microsoft.AspNetCore.Mvc;
using ListenerAPI.Interfaces;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ListenerAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class Namespaces : Controller
    {
        private readonly ILogger<Namespaces> _logger;
        private readonly IK8SClient _k8SClient;

        public Namespaces(
            ILogger<Namespaces> logger,
            IK8SClient k8SClient
            )
        {
            _logger = logger;
            _k8SClient = k8SClient;
            _logger.LogInformation("Controllers/Namespaces constructed");
        }

        // GET: Namespaces
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> Get()
        {
            _logger.LogInformation("HTTP GET /Namespaces called");

            try
            {
                var namespacesList = await _k8SClient.GetNamespacesAsync();

                _logger.LogInformation($"Returned: " +
                                       string.Join(", ", namespacesList.ToArray()));
                return Ok(namespacesList);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Called failed with exception: {ex}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "Error retrieving the namespaces");
            }
        }
    }
}
