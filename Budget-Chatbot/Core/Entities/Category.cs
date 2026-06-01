using System.Transactions;

namespace BudgetChatbot.Core.Entities;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // true = wydatek, false = przychód
    public bool IsExpense { get; set; }

    // Relacje: W jednej kategorii może być wiele transakcji różnych użytkowników
    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}