using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Votor.Models;

namespace Votor.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

		[Route("/Home/Privacy", Name = "Privacy")]
		public IActionResult Privacy()
        {
            return View();
        }

		[Route("/Home/Terms", Name = "Terms")]
		public IActionResult Terms()
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
