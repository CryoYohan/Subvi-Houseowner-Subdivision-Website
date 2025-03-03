using Microsoft.AspNetCore.Mvc;

public class StaffController : Controller
{
    public IActionResult Dashboard()
    {
        var role = HttpContext.Request.Cookies["jwt"];
        if (role != "Staff") return RedirectToAction("Index", "Home");

        return View();
    }
}
