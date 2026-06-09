using BudgetChatbot.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Budget_Chatbot.Controllers;

public class AdminController : Controller
{
    private readonly AppDbContext _db;

    public AdminController(AppDbContext db)
    {
        _db = db;
    }

    private IActionResult? RequireAdmin()
    {
        if (HttpContext.Session.GetString("Role") != "Admin")
            return RedirectToAction("Login", "Account");
        return null;
    }

    public async Task<IActionResult> Index()
    {
        var redirect = RequireAdmin();
        if (redirect != null) return redirect;

        var users = await _db.Users
            .Select(u => new { u.Id, u.Username, u.Email, u.CreatedAt })
            .ToListAsync();

        return View(users.Select(u => new AdminUserViewModel
        {
            Id = u.Id,
            Username = u.Username,
            Email = u.Email,
            CreatedAt = u.CreatedAt
        }).ToList());
    }

    [HttpPost]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var redirect = RequireAdmin();
        if (redirect != null) return redirect;

        var user = await _db.Users.FindAsync(id);
        if (user != null)
        {
            var histories = _db.ChatHistories.Where(h => h.UserId == id);
            _db.ChatHistories.RemoveRange(histories);

            var transactions = _db.Transactions.Where(t => t.UserId == id);
            _db.Transactions.RemoveRange(transactions);

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();
        }

        return RedirectToAction("Index");
    }
}

public class AdminUserViewModel
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
