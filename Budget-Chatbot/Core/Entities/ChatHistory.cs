namespace BudgetChatbot.Core.Entities;

public class ChatHistory
{
    public int Id { get; set; }

    // Rola: "User", "Bot" (lub "Assistant"), "System"
    public string Role { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Relacja: Historia zawsze należy do konkretnego użytkownika
    public int UserId { get; set; }
    public virtual User User { get; set; } = null!;
}