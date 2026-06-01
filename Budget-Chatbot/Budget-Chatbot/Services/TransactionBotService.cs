using AI.Integration.Services;
using BudgetChatbot.Core.DTOs;
using BudgetChatbot.Core.Entities;
using BudgetChatbot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BudgetChatbot.Services;

public class TransactionBotService
{
    private readonly AppDbContext _dbContext;
    private readonly LlmService _llmService;

    // Wstrzykujemy zarówno połączenie z bazą, jak i nasz serwis AI
    public TransactionBotService(AppDbContext dbContext, LlmService llmService)
    {
        _dbContext = dbContext;
        _llmService = llmService;
    }

    public async Task<Transaction?> ProcessMessageAndSaveAsync(int userId, string userMessage)
    {
        // 1. Pobranie słownika kategorii z bazy
        var categories = await _dbContext.Categories.ToListAsync();

        // Zabezpieczenie przed brakiem kategorii
        if (!categories.Any()) return null;

        // Tworzymy tekstową listę kategorii dla modelu, np. "1 - Jedzenie (Wydatek), 2 - Wypłata (Przychód)"
        var categoriesList = string.Join(", ", categories.Select(c => $"{c.Id} - {c.Name} ({(c.IsExpense ? "Wydatek" : "Przychód")})"));

        // 2. Zbudowanie inteligentnego promptu systemowego
        string systemPrompt = $@"Jesteś inteligentnym parserem finansowym. 
Twoim zadaniem jest wyciągnięcie danych z wiadomości użytkownika.
Dostępne kategorie w systemie: {categoriesList}.

Zwróć odpowiedź WYŁĄCZNIE jako czysty JSON, bez znaczników markdown, według struktury:
{{
  ""amount"": 0.0,
  ""categoryId"": 0,
  ""description"": ""string""
}}";

        // 3. Zapytanie do lokalnego modelu (Bielik)
        string llmResponse = await _llmService.AskInJsonModeAsync(systemPrompt, userMessage);

        try
        {
            // 4. Deserializacja JSON-a do naszego czystego kontraktu DTO
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var parsedDto = JsonSerializer.Deserialize<ParsedTransactionDto>(llmResponse, options);

            if (parsedDto == null || parsedDto.CategoryId == 0) return null;

            // 5. Mapowanie na encję bazodanową i zapis w SQL Serverze
            var transaction = new Transaction
            {
                UserId = userId, // W przyszłości pobierzesz to z tokena JWT
                CategoryId = parsedDto.CategoryId,
                Amount = parsedDto.Amount,
                Description = parsedDto.Description,
                Date = DateTime.UtcNow
            };

            _dbContext.Transactions.Add(transaction);
            await _dbContext.SaveChangesAsync();

            return transaction;
        }
        catch (JsonException ex)
        {
            // Opcjonalne logowanie błędu, gdyby model zhallucynował składnię
            Console.WriteLine($"Błąd parsowania JSON z modelu: {ex.Message}");
            return null;
        }
    }
}