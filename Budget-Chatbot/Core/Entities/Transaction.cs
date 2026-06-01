namespace BudgetChatbot.Core.Entities;

public class Transaction
{
    public int Id { get; set; }

    // Precyzyjny typ danych dla finansów
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;

    // Klucz obcy i relacja do Użytkownika
    public int UserId { get; set; }
    public virtual User User { get; set; } = null!;

    // Klucz obcy i relacja do Kategorii
    public int CategoryId { get; set; }
    public virtual Category Category { get; set; } = null!;
}