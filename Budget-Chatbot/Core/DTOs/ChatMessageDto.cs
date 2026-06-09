namespace BudgetChatbot.Core.DTOs;

public class ChatMessageDto
{
    public string SessionId { get; set; } = string.Empty;
    public string ChatTitle { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
