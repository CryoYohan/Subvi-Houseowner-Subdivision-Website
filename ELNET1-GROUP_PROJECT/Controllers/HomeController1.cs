using Microsoft.AspNetCore.Mvc;
using System.Text.Encodings.Web;

namespace MvcMovie.Controllers;

public class HelloWorldController : Controller
{
    // 
    // GET: /HelloWorld/
    public IActionResult Index()
    {
        return View();
    }
    // 
    // GET: /HelloWorld/Welcome/ 
    public string Welcome(string name = "Unknown", int ID = 1)
    {
        return HtmlEncoder.Default.Encode($"Hello {name}, ID: {ID}");
    }
    //
    // GET: /admin/
    [HttpGet]
    public ContentResult Admin()
    {
        return new ContentResult
        {
            ContentType = "text/html",
            Content = "<div>Hello World</div>"
        };
    }
}