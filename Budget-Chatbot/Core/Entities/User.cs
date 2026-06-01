namespace BudgetChatbot.Core.Entities;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Pełne nawigacje relacyjne do wszystkich tabel powiązanych
    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public virtual ICollection<RecurringTransaction> RecurringTransactions { get; set; } = new List<RecurringTransaction>();
    public virtual ICollection<ChatHistory> ChatHistories { get; set; } = new List<ChatHistory>();
}