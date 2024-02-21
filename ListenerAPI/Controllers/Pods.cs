using System;
using System.Threading.Tasks;
using ListenerAPI.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

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

                _logger.LogInformation($"Returned: " +
                                       string.Join(", ", podsList.ToArray()));
                return Ok(podsList);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Called failed with exception: {ex}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "Error retrieving the pods");
            }
        }

        //// GET api/<PodsController>/5
        //[HttpGet("{id}")]
        //public string Get(int id)
        //{
        //    return "value";
        //}

        //// POST api/<PodsController>
        //[HttpPost]
        //public void Post([FromBody] string value)
        //{
        //}

        //// PUT api/<PodsController>/5
        //[HttpPut("{id}")]
        //public void Put(int id, [FromBody] string value)
        //{
        //}

        //// DELETE api/<PodsController>/5
        //[HttpDelete("{id}")]
        //public void Delete(int id)
        //{
        //}
    }
}
