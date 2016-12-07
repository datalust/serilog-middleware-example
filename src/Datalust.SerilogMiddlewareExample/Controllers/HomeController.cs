using System;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Datalust.SerilogMiddlewareExample.Controllers
{
    public class HomeController : Controller
    {
        readonly ILogger _log = Log.ForContext<HomeController>();

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About([FromQuery] bool crash = false)
        {
            _log.Information("Hello from the Home controller");

            if (crash)
            {
                throw new DivideByZeroException();
            }

            ViewData["Message"] = "Your application description page.";
            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
