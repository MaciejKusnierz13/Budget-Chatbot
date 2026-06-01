namespace BudgetChatbot.Core.Entities;

public class RecurringTransaction
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }
    public DateTime NextRunDate { get; set; }

    // Interwał czasowy, np. "Monthly", "Weekly", "Yearly"
    
    public string Interval { get; set; } = string.Empty;

    // Relacje (tak jak w zwykłej transakcji)
    public int UserId { get; set; }
    public virtual User User { get; set; } = null!;

    public int CategoryId { get; set; }
    public virtual Category Category { get; set; } = null!;
}