using System.Diagnostics;
using Budget_Chatbot.Models;
using Microsoft.AspNetCore.Mvc;

namespace Budget_Chatbot.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    private IActionResult? RequireLogin()
    {
        if (HttpContext.Session.GetString("Username") == null)
            return RedirectToAction("Login", "Account");
        return null;
    }

    public IActionResult Index()
    {
        var redirect = RequireLogin();
        if (redirect != null) return redirect;

        ViewBag.Username = HttpContext.Session.GetString("Username");
        ViewBag.UserId = HttpContext.Session.GetInt32("UserId");
        return View();
    }

    public IActionResult Charts()
    {
        var redirect = RequireLogin();
        if (redirect != null) return redirect;

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
