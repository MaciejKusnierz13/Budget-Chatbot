namespace BudgetChatbot.Core.Entities;

public class ChatHistory
{
    public int Id { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public int UserId { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public string ChatTitle { get; set; } = string.Empty;
    public virtual User User { get; set; } = null!;
}
