using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using ELNET1_GROUP_PROJECT.Data;
using ELNET1_GROUP_PROJECT.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Linq;

[Route("staff")]
public class StaffController : Controller
{
    private readonly MyAppDBContext _context;

    public StaffController(MyAppDBContext context)
    {
        _context = context;
        ViewData["Layout"] = "_StaffLayout";
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

    private string GetUserRoleFromToken()
    {
        var token = HttpContext.Request.Cookies["jwt"];
        if (string.IsNullOrEmpty(token)) return null;

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadToken(token) as JwtSecurityToken;

        return jwtToken?.Claims.FirstOrDefault(c => c.Type == "Role")?.Value;
    }

    public IActionResult Landing()
    {
        var role = HttpContext.Request.Cookies["UserRole"];
        if (role != "Staff")
        {
            return RedirectToAction("Landing");
        }
        return View();
    }

    [HttpGet("")]
    [HttpGet("dashboard")]
    public IActionResult Dashboard()
    {
        var role = GetUserRoleFromToken();
        if (string.IsNullOrEmpty(role) || role != "Staff")
        {
            return RedirectToAction("Landing");
        }
        return View();
    }

    [HttpGet("pass/visitors")]
    public IActionResult VisitorsPass()
    {
        RefreshJwtCookies();
        var role = GetUserRoleFromToken();
        if (string.IsNullOrEmpty(role) || role != "Staff")
        {
            return RedirectToAction("landing", "Home");
        }
        return View();
    }

    [HttpGet("vehicle/registration")]
    public IActionResult VehicleRegistration()
    {
        RefreshJwtCookies();
        var role = HttpContext.Request.Cookies["UserRole"];
        if (string.IsNullOrEmpty(role) || role != "Staff")
        {
            return RedirectToAction("landing", "Home");
        }
        return View();
    }

    [HttpGet("requests/reservation")]
    public IActionResult ReservationRequests()
    {
        RefreshJwtCookies();
        var role = GetUserRoleFromToken();
        if (string.IsNullOrEmpty(role) || role != "Staff")
        {
            return RedirectToAction("Landing");
        }
        return View();
    }

    [HttpGet("requests/services")]
    public IActionResult ServiceRequests()
    {
        RefreshJwtCookies();
        var role = GetUserRoleFromToken();
        if (string.IsNullOrEmpty(role) || role != "Staff")
        {
            return RedirectToAction("Landing");
        }
        return View();
    }

    [HttpGet("bills_and_payments")]
    public IActionResult BillsAndPayments()
    {
        RefreshJwtCookies();
        var role = GetUserRoleFromToken();
        if (string.IsNullOrEmpty(role) || role != "Staff")
        {
            return RedirectToAction("Landing");
        }
        return View();
    }

    [HttpGet("reports")]
    public IActionResult Reports()
    {
        RefreshJwtCookies();
        var role = GetUserRoleFromToken();
        if (string.IsNullOrEmpty(role) || role != "Staff")
        {
            return RedirectToAction("Landing");
        }
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
