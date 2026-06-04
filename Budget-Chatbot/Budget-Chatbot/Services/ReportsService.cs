using BudgetChatbot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Budget_Chatbot.Services;

public class ReportsService
{
    private readonly AppDbContext _db;

    public ReportsService(AppDbContext db)
    {
        _db = db;
    }

    // 1. SALDO KONTA (zwraca aktualne saldo konta)
    public decimal GetBalance(int userId)
    {
        return _db.Transactions
            .Include(t => t.Category)
            .Where(t => t.UserId == userId)
            .Sum(t => t.Category.IsExpense ? -t.Amount : t.Amount);
    }

    // 2. WYKRES SALDA (zwraca historię salda konta z podanego okresu)
    public object GetBalanceChart(int userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var end = (endDate ?? DateTime.UtcNow).Date.AddDays(1);
        var start = (startDate ?? end.AddMonths(-1)).Date;

        // saldo przed początkiem zakresu
        decimal startingBalance = _db.Transactions
            .Include(t => t.Category)
            .Where(t => t.UserId == userId && t.Date < start)
            .Sum(t => t.Category.IsExpense ? -t.Amount : t.Amount);

        // zmiany w wybranym zakresie
        var dailyChanges = _db.Transactions
            .Include(t => t.Category)
            .Where(t => t.UserId == userId
                        && t.Date >= start
                        && t.Date < end)
            .GroupBy(t => t.Date.Date)
            .Select(g => new
            {
                Date = g.Key,
                Change = g.Sum(x => x.Category.IsExpense ? -x.Amount : x.Amount)
            })
            .OrderBy(x => x.Date)
            .ToList();

        decimal runningBalance = startingBalance;

        var result = dailyChanges
            .Select(x =>
            {
                runningBalance += x.Change;

                return new
                {
                    Date = x.Date,
                    Balance = runningBalance
                };
            })
            .ToList();

        return result;
    }

    // 6. UDZIAŁ KATEGORII WYDATKÓW W CZASIE
    public object GetWeeklyExpenseCategoryShare(int userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var end = endDate ?? DateTime.UtcNow;
        var start = startDate ?? end.AddMonths(-1);

        var data = _db.Transactions
            .Include(t => t.Category)
            .Where(t => t.UserId == userId
                        && t.Category.IsExpense
                        && t.Date >= start
                        && t.Date <= end)
            .ToList();

        var grouped = data
            .GroupBy(t =>
            {
                var week = System.Globalization.ISOWeek.GetWeekOfYear(t.Date);
                var year = System.Globalization.ISOWeek.GetYear(t.Date);
                return new { year, week };
            })
            .Select(g =>
            {
                var total = g.Sum(x => x.Amount);

                // 1. Deklarujemy i przygotowujemy słownik wcześniej
                Dictionary<string, decimal> categoryShare;

                if (total == 0)
                {
                    categoryShare = new Dictionary<string, decimal>();
                }
                else
                {
                    categoryShare = g
                        .GroupBy(x => x.Category.Name)
                        .ToDictionary(
                            cg => cg.Key,
                            // Math.Round automatycznie wybierze przeciążenie dla decimal
                            cg => Math.Round((cg.Sum(x => x.Amount) / total) * 100, 2)
                        );
                }

                // 2. Jeden, wspólny punkt zwrotu – kompilator bez problemu dopasuje typ
                return new
                {
                    Year = g.Key.year,
                    WeekNumber = g.Key.week,
                    Week = $"{g.Key.year}-W{g.Key.week:00}",
                    Categories = categoryShare
                };
            })
            .OrderBy(x => x.Year)
            .ThenBy(x => x.WeekNumber)
            .ToList();

        return grouped;
    }

    // 7. TOP 10 WYDATKÓW
    public object GetTopExpenses(int userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var end = endDate ?? DateTime.UtcNow;
        var start = startDate ?? end.AddMonths(-1);

        return _db.Transactions
            .Include(t => t.Category)
            .Where(t => t.UserId == userId
                        && t.Category.IsExpense
                        && t.Date >= start
                        && t.Date <= end)
            .OrderByDescending(t => t.Amount)
            .Take(10)
            .Select(t => new
            {
                t.Amount,
                t.Description,
                Category = t.Category.Name,
                t.Date
            })
            .ToList();
    }
}