using ELNET1_GROUP_PROJECT.Data;
using Microsoft.AspNetCore.Mvc;

namespace Subvi.Controllers
{
    public class UserController : Controller
    {
        private readonly MyAppDBContext _context;
        public UserController(MyAppDBContext context)
        {
            _context = context; 
        }
        public IActionResult Index()
        {
            return View();
        }
    }
}
