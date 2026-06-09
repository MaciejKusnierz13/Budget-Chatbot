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
        {
            if (HttpContext.Session.GetString("Role") == "Admin")
                return RedirectToAction("Index", "Admin");
            return RedirectToAction("Index", "Home");
        }
        return View();
    }

    [HttpPost]
    public IActionResult Login(string username, string? password)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            ViewBag.Error = "Podaj nazwę użytkownika.";
            return View();
        }

        if (username.Trim() == "admin" && password == "admin123")
        {
            HttpContext.Session.SetString("Role", "Admin");
            HttpContext.Session.SetString("Username", "admin");
            return RedirectToAction("Index", "Admin");
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
        HttpContext.Session.SetString("Role", "User");

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }
}
