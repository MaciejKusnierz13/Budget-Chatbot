using BudgetChatbot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Budget_Chatbot.Services;

// DTO (Struktury danych przesyłane do przeglądarki)
public class CategoryAmountDto
{
    public string CategoryName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsExpense { get; set; }
}

public class TimePeriodAmountDto
{
    public string Period { get; set; } = string.Empty;
    public decimal TotalExpenses { get; set; }
    public decimal TotalIncomes { get; set; }
}

public class AdvancedChartsService
{
    private readonly AppDbContext _db;

    public AdvancedChartsService(AppDbContext db)
    {
        _db = db;
    }

    // 1. Wykres słupkowy: Kategorie
    public List<CategoryAmountDto> GetCategoryBarChart(int userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var end = endDate ?? DateTime.UtcNow;
        var start = startDate ?? end.AddMonths(-1);

        return _db.Transactions
            .Include(t => t.Category)
            .Where(t => t.UserId == userId && t.Date >= start && t.Date <= end)
            .GroupBy(t => new { t.Category.Name, t.Category.IsExpense })
            .Select(g => new CategoryAmountDto
            {
                CategoryName = g.Key.Name,
                IsExpense = g.Key.IsExpense,
                Amount = g.Sum(t => t.Amount)
            })
            .OrderByDescending(x => x.Amount)
            .ToList();
    }

    // 2. Wykres liniowy: Kategorie w czasie
    public object GetCategoryLineChartOverTime(int userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var end = endDate ?? DateTime.UtcNow;
        var start = startDate ?? end.AddMonths(-1);

        var data = _db.Transactions
            .Include(t => t.Category)
            .Where(t => t.UserId == userId && t.Date >= start && t.Date <= end)
            .ToList();

        return data
            .GroupBy(t => new { DateStr = t.Date.ToString("yyyy-MM-dd"), t.Category.Name, t.Category.IsExpense })
            .Select(g => new
            {
                Date = g.Key.DateStr,
                Category = g.Key.Name,
                IsExpense = g.Key.IsExpense,
                Total = g.Sum(t => t.Amount)
            })
            .OrderBy(x => x.Date)
            .ToList();
    }

    // 3. Wykres słupkowy sumaryczny: Czas
    public List<TimePeriodAmountDto> GetSummaryBarChartOverTime(int userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var end = endDate ?? DateTime.UtcNow;
        var start = startDate ?? end.AddMonths(-6);

        var data = _db.Transactions
            .Include(t => t.Category)
            .Where(t => t.UserId == userId && t.Date >= start && t.Date <= end)
            .ToList();

        return data
            .GroupBy(t => t.Date.ToString("yyyy-MM"))
            .Select(g => new TimePeriodAmountDto
            {
                Period = g.Key,
                TotalExpenses = g.Where(x => x.Category.IsExpense).Sum(x => x.Amount),
                TotalIncomes = g.Where(x => !x.Category.IsExpense).Sum(x => x.Amount)
            })
            .OrderBy(x => x.Period)
            .ToList();
    }
}