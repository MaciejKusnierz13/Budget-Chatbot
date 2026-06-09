using BudgetChatbot.Infrastructure.Data;
using BudgetChatbot.Core.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Budget_Chatbot.Controllers;

public class AccountController : Controller
{
    private readonly AppDbContext _db;

    public AccountController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (HttpContext.Session.GetString("Username") != null)
            return RedirectToAction("Index", "Home");

        return View();
    }

    [HttpPost]
    public IActionResult Login(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            ViewBag.Error = "Podaj nazwę użytkownika.";
            return View();
        }

        username = username.Trim();

        var user = _db.Users.FirstOrDefault(u => u.Username == username);

        if (user == null)
        {
            user = new User
            {
                Username = username,
                Email = username.ToLower() + "@budgetchatbot.local"
            };
            _db.Users.Add(user);
            _db.SaveChanges();
        }

        HttpContext.Session.SetInt32("UserId", user.Id);
        HttpContext.Session.SetString("Username", user.Username);

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }
}
