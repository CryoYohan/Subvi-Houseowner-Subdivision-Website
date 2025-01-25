using Microsoft.AspNetCore.Mvc;
using System.Text.Encodings.Web;

namespace ELNET1_GROUP_PROJECT.Controllers
{
    public class HelloWorldController : Controller
    {
        //
        // GET: /HelloWorld/
        public IActionResult Index()
        {
            return View();
        }

        //
        // GET: /HelloWorld/Test  
        public  IActionResult Test()
        {
            return View();
        }

        // 
        // GET: /HelloWorld/Welcome/ 
        public string Welcome(string name ="Unknown", int numtimes=1)
        {
            return HtmlEncoder.Default.Encode($"Hello {name}! Your age is {numtimes}");
        }
    }
}
