using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ListenerAPI.Models;
using Microsoft.Extensions.Logging;

namespace ListenerAPI.Controllers
{
  public class Home : Controller
  {
    public Home(ILogger<Home> logger)
    {
      logger.LogInformation("Controllers/Home constructed");
    }

    public IActionResult Index()
    {
      return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
      return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
  }

}
