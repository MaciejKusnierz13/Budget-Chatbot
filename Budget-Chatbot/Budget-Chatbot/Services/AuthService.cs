using BudgetChatbot.Core.DTOs;
using BudgetChatbot.Core.Entities;
using BudgetChatbot.Infrastructure.Data;
using Core.DTOs;
using Microsoft.EntityFrameworkCore;

namespace BudgetChatbot.Services;

public class AuthService
{
    private readonly AppDbContext _db;

    public AuthService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> RegisterAsync(RegisterDto dto)
    {
        bool exists = await _db.Users
            .AnyAsync(x => x.Username == dto.Username);

        if (exists)
            return false;

        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            Password = dto.Password
        };

        _db.Users.Add(user);

        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<User?> LoginAsync(LoginDto dto)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(x =>
                x.Username == dto.Username &&
                x.Password == dto.Password);

        return user;
    }
}