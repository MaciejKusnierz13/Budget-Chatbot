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

    public TransactionBotService(AppDbContext dbContext, LlmService llmService)
    {
        _dbContext = dbContext;
        _llmService = llmService;
    }

    public async Task<Transaction?> ProcessMessageAndSaveAsync(int userId, string userMessage)
    {
        Console.WriteLine($"\n--- START DEBUGOWANIA ---");
        Console.WriteLine($"[KROK 1] Otrzymana wiadomość od usera: '{userMessage}'");

        // 1. Pobranie słownika kategorii z bazy
        var categories = await _dbContext.Categories.ToListAsync();
        Console.WriteLine($"[KROK 2] Znaleziono kategorii w bazie: {categories.Count}");

        if (!categories.Any())
        {
            Console.WriteLine("[STOP] Baza kategorii jest pusta! Przerywam działanie.");
            return null;
        }

        var categoriesList = string.Join(", ", categories.Select(c => $"{c.Id} - {c.Name} ({(c.IsExpense ? "Wydatek" : "Przychód")})"));

        string systemPrompt = $@"Zamień wiadomość użytkownika na obiekt JSON.
Dostępne kategorie: {categoriesList}

User: Kupiłem chleb za 5.50 zł
Assistant: {{""amount"": 5.50, ""categoryId"": 1, ""description"": ""chleb""}}

User: Bilet na tramwaj 80 PLN
Assistant: {{""amount"": 80.00, ""categoryId"": 2, ""description"": ""bilet na tramwaj""}}

User: Wypłata 5000 zł
Assistant: {{""amount"": 5000.00, ""categoryId"": 3, ""description"": ""wypłata""}}

Odpowiedz TYLKO i WYŁĄCZNIE wygenerowanym obiektem JSON dla najnowszej wiadomości. Nie używaj narzędzi.";

        Console.WriteLine("[KROK 3] Wysyłam zapytanie do LM Studio...");
        string llmResponse = await _llmService.AskInJsonModeAsync(systemPrompt, userMessage);
        Console.WriteLine($"[KROK 4] Odebrano odpowiedź od modelu. Długość: {llmResponse.Length} znaków.");

        int startIndex = llmResponse.IndexOf('{');
        int endIndex = llmResponse.LastIndexOf('}');

        if (startIndex >= 0 && endIndex > startIndex)
        {
            llmResponse = llmResponse.Substring(startIndex, endIndex - startIndex + 1);
            Console.WriteLine($"[KROK 5] Wycięty tekst do parsowania: {llmResponse}");
        }
        else
        {
            Console.WriteLine($"[STOP] Nie znaleziono klamer JSON w odpowiedzi! Surowy tekst: \n{llmResponse}");
            return null;
        }

        try
        {
            // --- NOWE: Kuloodporne sprawdzanie typu ValueKind ---
            using (var doc = JsonDocument.Parse(llmResponse))
            {
                if (doc.RootElement.TryGetProperty("arguments", out var argumentsElement))
                {
                    Console.WriteLine("[KROK 5.5] Wykryto format narzędziowy Bielika! Wyciągam wewnętrzny JSON.");

                    // Jeśli to tekst (jak za pierwszym razem)
                    if (argumentsElement.ValueKind == JsonValueKind.String)
                    {
                        llmResponse = argumentsElement.GetString() ?? llmResponse;
                    }
                    // Jeśli to obiekt (jak za drugim razem)
                    else if (argumentsElement.ValueKind == JsonValueKind.Object)
                    {
                        llmResponse = argumentsElement.GetRawText();
                    }
                }
            }

            // 5. Deserializacja właściwego JSON-a do naszego czystego kontraktu DTO
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var parsedDto = JsonSerializer.Deserialize<ParsedTransactionDto>(llmResponse, options);

            if (parsedDto == null)
            {
                Console.WriteLine("[STOP] Deserializacja zwróciła null!");
                return null;
            }

            if (parsedDto.CategoryId == 0)
            {
                Console.WriteLine($"[STOP] Parsowanie udane, ale CategoryId = 0. Obecny tekst: {llmResponse}");
                return null;
            }

            Console.WriteLine($"[KROK 6] Sukces! Kwota: {parsedDto.Amount}, Kategoria ID: {parsedDto.CategoryId}, Opis: {parsedDto.Description}");

            // 6. Mapowanie na encję bazodanową i zapis w SQL Serverze
            var transaction = new Transaction
            {
                UserId = userId,
                CategoryId = parsedDto.CategoryId,
                Amount = parsedDto.Amount,
                Description = parsedDto.Description,
                Date = DateTime.UtcNow
            };

            _dbContext.Transactions.Add(transaction);
            await _dbContext.SaveChangesAsync();
            Console.WriteLine("[KROK 7] Zapisano do bazy SQL Server!");

            return transaction;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"[STOP] Błąd parsowania System.Text.Json: {ex.Message}");
            return null;
        }
    }
}