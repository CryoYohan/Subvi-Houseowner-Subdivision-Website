using System.Diagnostics;
using ELNET1_GROUP_PROJECT.Models;
using Microsoft.AspNetCore.Mvc;

namespace ELNET1_GROUP_PROJECT.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult dashboard()
        {
            return View();
        }

        public IActionResult Calendar()
        {
            return View();
        }
        public IActionResult facilities()
        {
            return View();
        }

        public IActionResult bill()
        {
            return View();
        }

        public IActionResult services()
        {
            return View();
        }

        public IActionResult forums()
        {
            return View();
        }

        public IActionResult feedbacks()
        {
            return View();
        }

        public IActionResult Privacy()
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
