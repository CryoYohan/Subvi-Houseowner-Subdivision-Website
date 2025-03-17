using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ELNET1_GROUP_PROJECT.Models;

namespace ELNET1_GROUP_PROJECT.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        private string GetUserRoleFromToken()
        {
            var token = HttpContext.Request.Cookies["jwt"];
            if (string.IsNullOrEmpty(token)) return null;

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadToken(token) as JwtSecurityToken;

            return jwtToken?.Claims.FirstOrDefault(c => c.Type == "Role")?.Value;
        }

        //Resetting the cookies time
        private void RefreshJwtCookies()
        {
            var token = Request.Cookies["jwt"];
            var role = Request.Cookies["UserRole"];
            var id = Request.Cookies["Id"];

            if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(role) && !string.IsNullOrEmpty(id))
            {
                // Reset cookies with updated expiration
                var expiryMinutes = 15;  // Reset to 15 minutes

                var options = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddMinutes(expiryMinutes)
                };

                Response.Cookies.Append("jwt", token, options);
                Response.Cookies.Append("UserRole", role, new CookieOptions
                {
                    HttpOnly = false,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddMinutes(expiryMinutes)
                });
                Response.Cookies.Append("Id", id, new CookieOptions
                {
                    HttpOnly = false,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddMinutes(expiryMinutes)
                });
            }
        }

        public IActionResult landing()
        {
            RefreshJwtCookies();
            var role = GetUserRoleFromToken();
            if (!string.IsNullOrEmpty(role))
            {
                return RedirectToRoleDashboard(role);
            }
            return View();
        }

        public IActionResult AboutUs()
        {
            return View();
        }

        public IActionResult Contacts()
        {
            return View();
        }

        public IActionResult dashboard()
        {
            RefreshJwtCookies();
            var role = GetUserRoleFromToken();
            if (role != "Homeowner")
            {
                return RedirectToAction("landing"); // Redirect unauthorized users
            }

            return View(); // Load the `dashboard.cshtml` view
        }

        private IActionResult RedirectToRoleDashboard(string role)
        {
            RefreshJwtCookies();
            return role switch
            {
                "Admin" => RedirectToAction("Dashboard", "Admin"),
                "Homeowner" => RedirectToAction("dashboard", "Home"),
                "Staff" => RedirectToAction("Dashboard", "Staff"),
                _ => RedirectToAction("landing", "Home")
            };
        }

        public IActionResult Calendar()
        {
            RefreshJwtCookies();
            return View();
        }

        public IActionResult Facilities()
        {
            RefreshJwtCookies();
            return View();
        }

        public IActionResult Bill()
        {
            RefreshJwtCookies();
            return View();
        }

        public IActionResult Services()
        {
            RefreshJwtCookies();
            return View();
        }

        public IActionResult Forums()
        {
            RefreshJwtCookies();
            return View();
        }

        public IActionResult Feedbacks()
        {
            RefreshJwtCookies();
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
