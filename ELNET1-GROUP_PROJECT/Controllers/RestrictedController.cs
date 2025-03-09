using Microsoft.AspNetCore.Mvc;

namespace Subvi.Controllers
{
    public class RestrictedController : Controller
    {
        public IActionResult UnauthorizedAccess()
        {
            return View();
        }
    }
}
