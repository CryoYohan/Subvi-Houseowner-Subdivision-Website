using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using ELNET1_GROUP_PROJECT.Data;
using ELNET1_GROUP_PROJECT.Models;
using Microsoft.AspNetCore.Mvc;

public class StaffController : Controller
{
    private readonly MyAppDBContext _context;

    public StaffController(MyAppDBContext context)
    {
        _context = context;
        ViewData["Layout"] = "_Layout";
    }

    private string GetUserRoleFromToken()
    {
        var token = HttpContext.Request.Cookies["jwt"];
        if (string.IsNullOrEmpty(token)) return null;

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadToken(token) as JwtSecurityToken;

        return jwtToken?.Claims.FirstOrDefault(c => c.Type == "Role")?.Value;
    }

    public IActionResult Staffdashboard()
    {
        var role = GetUserRoleFromToken();
        if (role != "Staff")
        {
            return RedirectToAction("landing");
        }

        return View();
    }

    public IActionResult Calendar()
    {
        return View();
    }

    public IActionResult Facilities()
    {
        return View();
    }

    public IActionResult Bill()
    {
        return View();
    }

    public IActionResult Services()
    {
        return View();
    }

    public IActionResult Forums()
    {
        return View();
    }

    public IActionResult Feedbacks()
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
