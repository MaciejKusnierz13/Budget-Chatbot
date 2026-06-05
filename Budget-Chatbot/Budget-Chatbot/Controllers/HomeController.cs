using System.Diagnostics;
using Budget_Chatbot.Models;
using Microsoft.AspNetCore.Mvc;

namespace Budget_Chatbot.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        // =========================================================================
        // TUTAJ DOPISALIŚMY NOWĄ AKCJĘ DLA TWOICH WYKRESÓW
        // =========================================================================
        public IActionResult Charts()
        {
            return View(); // Ta linijka otworzy plik Views/Home/Charts.cshtml
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}